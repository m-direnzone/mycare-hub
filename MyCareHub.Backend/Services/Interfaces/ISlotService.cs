using MyCareHub.Backend.Models.DTOs.Slot;

namespace MyCareHub.Backend.Services.Interfaces;

public interface ISlotService
{
    /// <summary>
    /// Recupera solo gli slot liberi di un medico per la prenotazione del paziente.
    /// </summary>
    Task<IEnumerable<SlotDto>> GetSlotDisponibiliMedicoAsync(
        Guid medicoId,
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
