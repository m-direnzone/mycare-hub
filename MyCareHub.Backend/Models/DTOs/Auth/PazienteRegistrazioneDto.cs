using System.ComponentModel.DataAnnotations;

namespace MyCareHub.Backend.Models.DTOs.Auth;

/// <summary>
/// Payload inviato dal form di registrazione pubblica del paziente.
/// </summary>
public class PazienteRegistrazioneDto
{
    [Required(ErrorMessage = "L'email è obbligatoria.")]
    [EmailAddress(ErrorMessage = "Formato email non valido.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La password è obbligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La password deve essere di almeno 8 caratteri.")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Il nome è obbligatorio.")]
    [StringLength(100, ErrorMessage = "Il nome non può superare i 100 caratteri.")]
    public string Nome { get; set; } = null!;

    [Required(ErrorMessage = "Il cognome è obbligatorio.")]
    [StringLength(100, ErrorMessage = "Il cognome non può superare i 100 caratteri.")]
    public string Cognome { get; set; } = null!;

    [Required(ErrorMessage = "Il codice fiscale è obbligatorio.")]
    [RegularExpression(@"^[A-Z]{6}\d{2}[A-Z]\d{2}[A-Z]\d{3}[A-Z]$", ErrorMessage = "Formato Codice Fiscale non valido.")]
    public string CodiceFiscale { get; set; } = null!;

    [Required(ErrorMessage = "La data di nascita è obbligatoria.")]
    public DateOnly DataNascita { get; set; }

    [Phone(ErrorMessage = "Formato telefono non valido.")]
    [StringLength(20, ErrorMessage = "Il telefono non può superare i 20 caratteri.")]
    public string? Telefono { get; set; }
}
