using System.ComponentModel.DataAnnotations;

namespace MyCareHub.Backend.Models.DTOs.Prenotazione;

/// <summary>
/// Payload inviato dal paziente per confermare la visita.
/// Accetta uno slot esistente (SlotId) oppure un orario standard (DataOraInizio).
/// L'id del paziente viene estratto dal JWT lato backend.
/// </summary>
public class PrenotazioneCreateDto : IValidatableObject
{
    public Guid? SlotId { get; set; }

    [Required(ErrorMessage = "L'ID del medico è obbligatorio.")]
    public Guid MedicoId { get; set; }

    /// <summary>
    /// Inizio fascia oraria (30 minuti). Obbligatorio se SlotId non è valorizzato.
    /// </summary>
    public DateTimeOffset? DataOraInizio { get; set; }

    [StringLength(500, ErrorMessage = "Le note non possono superare i 500 caratteri.")]
    public string? NotePaziente { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if ((!SlotId.HasValue || SlotId == Guid.Empty) && !DataOraInizio.HasValue)
        {
            yield return new ValidationResult(
                "Seleziona uno slot orario valido.",
                [nameof(SlotId), nameof(DataOraInizio)]);
        }
    }
}
