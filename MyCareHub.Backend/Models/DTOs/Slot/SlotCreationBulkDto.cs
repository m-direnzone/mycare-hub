using System.ComponentModel.DataAnnotations;

namespace MyCareHub.Backend.Models.DTOs.Slot;

/// <summary>
/// Inserimento massivo di slot liberi in agenda (medico o segreteria).
/// </summary>
public class SlotCreationBulkDto : IValidatableObject
{
    [Required(ErrorMessage = "L'ID del medico è obbligatorio.")]
    public Guid MedicoId { get; set; }

    [Required(ErrorMessage = "La lista degli intervalli orari è obbligatoria.")]
    [MinLength(1, ErrorMessage = "Inserire almeno un intervallo orario.")]
    public List<TimeIntervalDto> SlotRichiesti { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < SlotRichiesti.Count; i++)
        {
            var slot = SlotRichiesti[i];
            if (slot.DataOraFine <= slot.DataOraInizio)
            {
                yield return new ValidationResult(
                    "La data/ora di fine deve essere successiva a quella di inizio.",
                    [nameof(SlotRichiesti)]);
            }

            if (slot.DataOraInizio < now)
            {
                yield return new ValidationResult(
                    "Non è possibile creare slot nel passato.",
                    [nameof(SlotRichiesti)]);
            }
        }
    }
}

/// <summary>
/// Intervallo temporale singolo all'interno di una creazione bulk.
/// </summary>
public class TimeIntervalDto
{
    [Required(ErrorMessage = "La data e ora di inizio sono obbligatorie.")]
    public DateTimeOffset DataOraInizio { get; set; }

    [Required(ErrorMessage = "La data e ora di fine sono obbligatorie.")]
    public DateTimeOffset DataOraFine { get; set; }
}
