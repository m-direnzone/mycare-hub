using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyCareHub.Backend.Models.DTOs.Auth;
using MyCareHub.Backend.Services.Interfaces;

namespace MyCareHub.Backend.Services.Implementations;

public class SupabaseAuthAdminService : ISupabaseAuthAdminService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SupabaseAuthAdminService> _logger;

    public SupabaseAuthAdminService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SupabaseAuthAdminService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<MedicoAuthCreateResult> CreateMedicoUserAsync(
        MedicoOnboardingDto dto,
        string passwordTemporanea,
        CancellationToken cancellationToken = default)
    {
        var (url, serviceRoleKey) = GetAdminConfig();
        var client = CreateAdminClient(url, serviceRoleKey);

        var body = new
        {
            email = dto.Email.Trim().ToLowerInvariant(),
            password = passwordTemporanea,
            email_confirm = true,
            user_metadata = new Dictionary<string, object?>
            {
                ["nome"] = dto.Nome.Trim(),
                ["cognome"] = dto.Cognome.Trim(),
                ["codice_fiscale"] = dto.CodiceFiscale.Trim().ToUpperInvariant(),
                ["data_nascita"] = dto.DataNascita.ToString("yyyy-MM-dd"),
                ["telefono"] = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim(),
                ["ruolo"] = "medico",
                ["must_change_password"] = true
            },
            app_metadata = new Dictionary<string, object?>
            {
                ["ruolo"] = "medico"
            }
        };

        using var response = await client.PostAsJsonAsync(
            "auth/v1/admin/users",
            body,
            JsonOptions,
            cancellationToken);

        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Creazione utente medico fallita: {Status} {Body}", response.StatusCode, payload);
            throw new InvalidOperationException(ExtractErrorMessage(payload, "Impossibile creare l'utente medico."));
        }

        using var doc = JsonDocument.Parse(payload);
        if (!doc.RootElement.TryGetProperty("id", out var idProp)
            || !Guid.TryParse(idProp.GetString(), out var userId))
        {
            throw new InvalidOperationException("Risposta Auth Admin senza id utente.");
        }

        return new MedicoAuthCreateResult
        {
            UserId = userId,
            Email = dto.Email.Trim().ToLowerInvariant()
        };
    }

    public async Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var (url, serviceRoleKey) = GetAdminConfig();
        var client = CreateAdminClient(url, serviceRoleKey);

        using var response = await client.DeleteAsync(
            $"auth/v1/admin/users/{userId}",
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var payload = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Eliminazione utente {UserId} fallita: {Status} {Body}",
                userId,
                response.StatusCode,
                payload);
        }
    }

    private (string Url, string ServiceRoleKey) GetAdminConfig()
    {
        var url = _configuration["Supabase:Url"]
            ?? throw new InvalidOperationException("Supabase:Url mancante in configurazione.");

        var serviceRoleKey =
            _configuration["Supabase:ServiceRoleKey"]
            ?? _configuration["SUPABASE_SERVICE_ROLE_KEY"]
            ?? Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");

        if (string.IsNullOrWhiteSpace(serviceRoleKey))
        {
            throw new InvalidOperationException(
                "Supabase:ServiceRoleKey mancante. Imposta la service role key per creare medici dal backend.");
        }

        return (url.TrimEnd('/'), serviceRoleKey);
    }

    private HttpClient CreateAdminClient(string url, string serviceRoleKey)
    {
        var client = _httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(url.EndsWith('/') ? url : url + "/");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", serviceRoleKey);
        client.DefaultRequestHeaders.Add("apikey", serviceRoleKey);
        return client;
    }

    private static string ExtractErrorMessage(string payload, string fallback)
    {
        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("msg", out var msg) && msg.GetString() is { Length: > 0 } m)
                return m;
            if (doc.RootElement.TryGetProperty("message", out var message) && message.GetString() is { Length: > 0 } m2)
                return m2;
            if (doc.RootElement.TryGetProperty("error_description", out var desc) && desc.GetString() is { Length: > 0 } m3)
                return m3;
        }
        catch (JsonException)
        {
        }

        return string.IsNullOrWhiteSpace(payload) ? fallback : payload;
    }
}
