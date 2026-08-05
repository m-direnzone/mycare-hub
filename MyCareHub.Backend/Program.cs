using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Repositories.Implementations;
using MyCareHub.Backend.Repositories.Interfaces;
using MyCareHub.Backend.Services.Implementations;
using MyCareHub.Backend.Services.Interfaces;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

var supabaseUrl = builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException("Supabase:Url mancante in configurazione.");
// Sempre publishable/anon key + JWT utente inoltrato: le operazioni rispettano RLS.
// La service role non è usata dal backend (creazione Auth medici → Edge Function).
var supabaseKey = builder.Configuration["Supabase:Key"]
    ?? throw new InvalidOperationException("Supabase:Key mancante in configurazione.");
var jwtIssuer = builder.Configuration["Supabase:JwtIssuer"]
    ?? $"{supabaseUrl}/auth/v1";
var jwtAudience = builder.Configuration["Supabase:JwtAudience"] ?? "authenticated";
var jwksUrl = builder.Configuration["Supabase:JwksUrl"]
    ?? $"{supabaseUrl}/auth/v1/.well-known/jwks.json";
var jwtSecret = builder.Configuration["Supabase:JwtSecret"];

builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();

var signingKeys = new List<SecurityKey>();
using (var httpClient = new HttpClient())
{
    var jwksJson = await httpClient.GetStringAsync(jwksUrl);
    signingKeys.AddRange(new JsonWebKeySet(jwksJson).GetSigningKeys());
}

if (!string.IsNullOrWhiteSpace(jwtSecret))
{
    signingKeys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)));
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,
            NameClaimType = "sub",
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                if (context.Principal?.Identity is not ClaimsIdentity identity)
                    return;

                await MapRuoloClaimsAsync(context, identity);
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(nameof(RuoloUtente.Paziente), policy =>
        policy.RequireRole(nameof(RuoloUtente.Paziente), "paziente"));

    options.AddPolicy(nameof(RuoloUtente.Medico), policy =>
        policy.RequireRole(nameof(RuoloUtente.Medico), "medico"));

    options.AddPolicy(nameof(RuoloUtente.Admin), policy =>
        policy.RequireRole(nameof(RuoloUtente.Admin), "admin"));
});

builder.Services.AddScoped<Client>(sp =>
{
    var options = new SupabaseOptions
    {
        AutoRefreshToken = false,
        AutoConnectRealtime = false
    };

    // Inoltra sempre il JWT utente così RLS (auth.uid() / is_current_user_admin) funziona.
    var http = sp.GetService<IHttpContextAccessor>()?.HttpContext;
    var authorization = http?.Request.Headers.Authorization.ToString();
    if (!string.IsNullOrWhiteSpace(authorization) &&
        authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        options.Headers["Authorization"] = authorization;
    }

    var client = new Client(supabaseUrl, supabaseKey, options);
    client.InitializeAsync().GetAwaiter().GetResult();
    return client;
});

builder.Services.AddScoped<IProfiloRepository, ProfiloRepository>();
builder.Services.AddScoped<IMedicoRepository, MedicoRepository>();
builder.Services.AddScoped<ISlotDisponibilitaRepository, SlotDisponibilitaRepository>();
builder.Services.AddScoped<IPrenotazioneRepository, PrenotazioneRepository>();
builder.Services.AddScoped<IRefertoMedicoRepository, RefertoMedicoRepository>();
builder.Services.AddScoped<ISpecializzazioneRepository, SpecializzazioneRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();

builder.Services.AddScoped<ISupabaseAuthAdminService, SupabaseAuthAdminService>();
builder.Services.AddScoped<IProfiloService, ProfiloService>();
builder.Services.AddScoped<ISlotService, SlotService>();
builder.Services.AddScoped<IPrenotazioneService, PrenotazioneService>();
builder.Services.AddScoped<IRefertoService, RefertoService>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:3000"];

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static async Task MapRuoloClaimsAsync(TokenValidatedContext context, ClaimsIdentity identity)
{
    if (HasAppRuoloClaim(identity))
        return;

    var ruolo = ExtractRuoloFromToken(context);
    if (string.IsNullOrWhiteSpace(ruolo))
        ruolo = await LookupRuoloFromProfiloAsync(context);

    if (string.IsNullOrWhiteSpace(ruolo))
        return;

    AddRuoloClaims(identity, ruolo);
}

static bool HasAppRuoloClaim(ClaimsIdentity identity)
{
    static bool IsAppRuolo(string value) =>
        value is "paziente" or "Paziente" or "medico" or "Medico" or "admin" or "Admin";

    return identity.FindAll(ClaimTypes.Role).Any(c => IsAppRuolo(c.Value))
        || identity.FindAll("role").Any(c => IsAppRuolo(c.Value))
        || identity.FindAll("ruolo").Any(c => IsAppRuolo(c.Value));
}

static void AddRuoloClaims(ClaimsIdentity identity, string ruolo)
{
    var normalized = NormalizeRuolo(ruolo);
    identity.AddClaim(new Claim(ClaimTypes.Role, ruolo));
    if (!string.Equals(normalized, ruolo, StringComparison.Ordinal))
        identity.AddClaim(new Claim(ClaimTypes.Role, normalized));

    // Compatibilità con token che espongono già "role"/"ruolo" lowercase.
    var lower = ruolo.ToLowerInvariant();
    if (lower is "paziente" or "medico" or "admin")
        identity.AddClaim(new Claim(ClaimTypes.Role, lower));
}

static string? ExtractRuoloFromToken(TokenValidatedContext context)
{
    // 1) Payload JWT grezzo (affidabile con JsonWebTokenHandler)
    if (context.SecurityToken is Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jwt)
    {
        var fromApp = ReadRuoloFromMetadataElement(TryGetJwtObject(jwt, "app_metadata"));
        if (!string.IsNullOrWhiteSpace(fromApp))
            return fromApp;

        var fromUser = ReadRuoloFromMetadataElement(TryGetJwtObject(jwt, "user_metadata"));
        if (!string.IsNullOrWhiteSpace(fromUser))
            return fromUser;

        if (jwt.TryGetPayloadValue("ruolo", out string? topRuolo) && !string.IsNullOrWhiteSpace(topRuolo))
            return topRuolo;
    }

    // 2) Claim serializzati come JSON string
    var fromAppClaim = ReadRuoloFromMetadataJson(context.Principal?.FindFirst("app_metadata")?.Value);
    if (!string.IsNullOrWhiteSpace(fromAppClaim))
        return fromAppClaim;

    var fromUserClaim = ReadRuoloFromMetadataJson(context.Principal?.FindFirst("user_metadata")?.Value);
    if (!string.IsNullOrWhiteSpace(fromUserClaim))
        return fromUserClaim;

    // 3) Claim piatti
    return context.Principal?.FindFirst("ruolo")?.Value
        ?? context.Principal?.Claims
            .FirstOrDefault(c =>
                c.Type.EndsWith("ruolo", StringComparison.OrdinalIgnoreCase) &&
                c.Value is "paziente" or "medico" or "admin")
            ?.Value;
}

static JsonElement? TryGetJwtObject(
    Microsoft.IdentityModel.JsonWebTokens.JsonWebToken jwt,
    string propertyName)
{
    if (!jwt.TryGetPayloadValue(propertyName, out JsonElement element))
        return null;

    return element.ValueKind == JsonValueKind.Object ? element : null;
}

static string? ReadRuoloFromMetadataElement(JsonElement? element)
{
    if (element is null)
        return null;

    var root = element.Value;
    if (root.TryGetProperty("ruolo", out var ruoloProp))
        return ruoloProp.GetString();
    if (root.TryGetProperty("role", out var roleProp))
    {
        var role = roleProp.GetString();
        // Ignora il ruolo Postgres "authenticated" di Supabase.
        if (role is "paziente" or "medico" or "admin"
            or "Paziente" or "Medico" or "Admin")
            return role;
    }

    return null;
}

static string? ReadRuoloFromMetadataJson(string? json)
{
    if (string.IsNullOrWhiteSpace(json))
        return null;

    try
    {
        using var doc = JsonDocument.Parse(json);
        return ReadRuoloFromMetadataElement(doc.RootElement);
    }
    catch (JsonException)
    {
        return null;
    }
}

static async Task<string?> LookupRuoloFromProfiloAsync(TokenValidatedContext context)
{
    var sub = context.Principal?.FindFirst("sub")?.Value
        ?? context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (!Guid.TryParse(sub, out var userId))
        return null;

    var profili = context.HttpContext.RequestServices.GetService<IProfiloRepository>();
    if (profili is null)
        return null;

    var profilo = await profili.GetByIdAsync(userId);
    if (profilo is null)
        return null;

    return profilo.Ruolo switch
    {
        RuoloUtente.Paziente => "paziente",
        RuoloUtente.Medico => "medico",
        RuoloUtente.Admin => "admin",
        _ => null
    };
}

static string NormalizeRuolo(string ruolo) => ruolo.ToLowerInvariant() switch
{
    "paziente" => nameof(RuoloUtente.Paziente),
    "medico" => nameof(RuoloUtente.Medico),
    "admin" => nameof(RuoloUtente.Admin),
    _ => ruolo
};
