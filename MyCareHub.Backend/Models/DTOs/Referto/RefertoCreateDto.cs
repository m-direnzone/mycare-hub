using System.ComponentModel.DataAnnotations;

namespace MyCareHub.Backend.Models.DTOs.Referto;

/// <summary>
/// Metadati del referto inviati insieme al PDF (multipart/form-data).
/// L'id del medico viene estratto dal JWT lato backend.
/// </summary>
public class RefertoCreateDto
{
    /// <summary>
    /// Opzionale se il documento è slegato da una visita specifica.
    /// </summary>
    public Guid? PrenotazioneId { get; set; }

    [Required(ErrorMessage = "L'ID del paziente è obbligatorio.")]
    public Guid PazienteId { get; set; }

    [Required(ErrorMessage = "Il titolo del referto è obbligatorio.")]
    [StringLength(100, ErrorMessage = "Il titolo non può superare i 100 caratteri.")]
    public string Titolo { get; set; } = null!;

    [StringLength(2000, ErrorMessage = "La descrizione non può superare i 2000 caratteri.")]
    public string? Descrizione { get; set; }
}
