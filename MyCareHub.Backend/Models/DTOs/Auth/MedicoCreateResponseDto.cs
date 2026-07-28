namespace MyCareHub.Backend.Models.DTOs.Auth;

/// <summary>
/// Risposta dopo la creazione completa di un medico da parte dell'admin.
/// </summary>
public class MedicoCreateResponseDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordTemporanea { get; set; } = string.Empty;
    public string Messaggio { get; set; } = string.Empty;
}
