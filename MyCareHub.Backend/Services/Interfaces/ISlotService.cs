using MyCareHub.Backend.Models.DTOs.Slot;

namespace MyCareHub.Backend.Services.Interfaces;

public interface ISlotService
{
    /// <summary>
    /// Fasce orarie standard (30 min, 07–13 / 14–18) libere per medico e paziente.
    /// </summary>
    Task<IEnumerable<SlotDto>> GetSlotDisponibiliMedicoAsync(
        Guid medicoId,
        Guid? pazienteId = null,
        DateTimeOffset? dataInizio = null,
        DateTimeOffset? dataFine = null);

    Task<IEnumerable<SlotDto>> GetAgendaMedicoAsync(
        Guid medicoId,
        DateTimeOffset? dataInizio = null,
        DateTimeOffset? dataFine = null);

    /// <summary>
    /// Genera massivamente gli slot orari per l'agenda del medico.
    /// </summary>
    Task<bool> GeneraAgendaBulkAsync(SlotCreationBulkDto dto);

    /// <summary>
    /// Elimina uno slot libero dall'agenda (fallisce se già prenotato).
    /// </summary>
    Task<bool> EliminaSlotAsync(Guid slotId, Guid medicoId);
}
