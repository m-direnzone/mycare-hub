using System.ComponentModel.DataAnnotations;

namespace MyCareHub.Backend.Models.DTOs.Auth;

/// <summary>
/// Payload inviato da Admin/Segreteria per registrare ufficialmente un medico.
/// </summary>
public class MedicoOnboardingDto
{
    [Required(ErrorMessage = "L'email è obbligatoria.")]
    [EmailAddress(ErrorMessage = "Formato email non valido.")]
    public string Email { get; set; } = null!;

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

    [Required(ErrorMessage = "La specializzazione medica è obbligatoria.")]
    [StringLength(150, ErrorMessage = "La specializzazione non può superare i 150 caratteri.")]
    public string Specializzazione { get; set; } = null!;

    [Required(ErrorMessage = "Il numero di iscrizione all'albo è obbligatorio.")]
    [StringLength(50, ErrorMessage = "Il numero di iscrizione all'albo non può superare i 50 caratteri.")]
    public string NumeroIscrizioneAlbo { get; set; } = null!;

    [StringLength(2000, ErrorMessage = "La biografia non può superare i 2000 caratteri.")]
    public string? Biografia { get; set; }

    [Required(ErrorMessage = "La tariffa della visita è obbligatoria.")]
    [Range(0.01, 10000.00, ErrorMessage = "La tariffa deve essere maggiore di zero.")]
    public decimal TariffaVisita { get; set; }
}
