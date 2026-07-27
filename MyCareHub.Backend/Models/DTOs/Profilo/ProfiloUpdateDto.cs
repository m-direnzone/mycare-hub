using System.ComponentModel.DataAnnotations;

namespace MyCareHub.Backend.Models.DTOs.Profilo;

/// <summary>
/// Payload per l'aggiornamento anagrafico del profilo autenticato.
/// Codice fiscale e ruolo non sono modificabili dal client.
/// </summary>
public class ProfiloUpdateDto
{
    [Required(ErrorMessage = "Il nome è obbligatorio.")]
    [StringLength(100, ErrorMessage = "Il nome non può superare i 100 caratteri.")]
    public string Nome { get; set; } = null!;

    [Required(ErrorMessage = "Il cognome è obbligatorio.")]
    [StringLength(100, ErrorMessage = "Il cognome non può superare i 100 caratteri.")]
    public string Cognome { get; set; } = null!;

    [Phone(ErrorMessage = "Formato telefono non valido.")]
    [StringLength(20, ErrorMessage = "Il telefono non può superare i 20 caratteri.")]
    public string? Telefono { get; set; }
}

/// <summary>
/// Payload per l'aggiornamento dei dettagli professionali del medico
/// (profilo medico autenticato o Admin).
/// </summary>
public class MedicoDettaglioUpdateDto
{
    [Required(ErrorMessage = "La specializzazione medica è obbligatoria.")]
    [StringLength(150, ErrorMessage = "La specializzazione non può superare i 150 caratteri.")]
    public string Specializzazione { get; set; } = null!;

    [StringLength(2000, ErrorMessage = "La biografia non può superare i 2000 caratteri.")]
    public string? Biografia { get; set; }

    [Required(ErrorMessage = "La tariffa della visita è obbligatoria.")]
    [Range(0.01, 10000.00, ErrorMessage = "La tariffa deve essere maggiore di zero.")]
    public decimal TariffaVisita { get; set; }

    public bool Attivo { get; set; } = true;
}
