using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using MyCareHub.Backend.Entities.Enums;
using Supabase;

var builder = WebApplication.CreateBuilder(args);

var supabaseUrl = builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException("Supabase:Url mancante in configurazione.");
var supabaseKey = builder.Configuration["Supabase:Key"]
    ?? throw new InvalidOperationException("Supabase:Key mancante in configurazione.");
var jwtIssuer = builder.Configuration["Supabase:JwtIssuer"]
    ?? $"{supabaseUrl}/auth/v1";
var jwtAudience = builder.Configuration["Supabase:JwtAudience"] ?? "authenticated";
var jwksUrl = builder.Configuration["Supabase:JwksUrl"]
    ?? $"{supabaseUrl}/auth/v1/.well-known/jwks.json";
var jwtSecret = builder.Configuration["Supabase:JwtSecret"];

builder.Services.AddHttpClient();

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
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is not ClaimsIdentity identity)
                    return Task.CompletedTask;

                MapAppMetadataRole(context, identity);
                return Task.CompletedTask;
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

builder.Services.AddScoped<Client>(_ =>
{
    var options = new SupabaseOptions
    {
        AutoRefreshToken = false,
        AutoConnectRealtime = false
    };

    var client = new Client(supabaseUrl, supabaseKey, options);
    client.InitializeAsync().GetAwaiter().GetResult();
    return client;
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static void MapAppMetadataRole(TokenValidatedContext context, ClaimsIdentity identity)
{
    string? ruolo = null;

    var appMetadataClaim = context.Principal?.FindFirst("app_metadata")?.Value;
    if (!string.IsNullOrWhiteSpace(appMetadataClaim))
    {
        try
        {
            using var doc = JsonDocument.Parse(appMetadataClaim);
            if (doc.RootElement.TryGetProperty("ruolo", out var ruoloProp))
                ruolo = ruoloProp.GetString();
            else if (doc.RootElement.TryGetProperty("role", out var roleProp))
                ruolo = roleProp.GetString();
        }
        catch (JsonException)
        {
        }
    }

    ruolo ??= context.Principal?.FindFirst("ruolo")?.Value;

    if (string.IsNullOrWhiteSpace(ruolo))
        return;

    identity.AddClaim(new Claim(ClaimTypes.Role, ruolo));
    identity.AddClaim(new Claim(ClaimTypes.Role, NormalizeRuolo(ruolo)));
}

static string NormalizeRuolo(string ruolo) => ruolo.ToLowerInvariant() switch
{
    "paziente" => nameof(RuoloUtente.Paziente),
    "medico" => nameof(RuoloUtente.Medico),
    "admin" => nameof(RuoloUtente.Admin),
    _ => ruolo
};
