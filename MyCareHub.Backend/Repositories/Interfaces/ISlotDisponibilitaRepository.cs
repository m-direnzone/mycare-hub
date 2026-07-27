using MyCareHub.Backend.Entities;

namespace MyCareHub.Backend.Repositories.Interfaces;

public interface ISlotDisponibilitaRepository
{
    Task<SlotDisponibilita?> GetByIdAsync(Guid id);

    /// <summary>
    /// Recupera gli slot di un medico, con filtri opzionali per range temporale e stato di prenotazione.
    /// </summary>
    Task<IEnumerable<SlotDisponibilita>> GetByMedicoIdAsync(
        Guid medicoId,
        DateTimeOffset? dataInizio = null,
        DateTimeOffset? dataFine = null,
        bool? isPrenotato = null);

    /// <summary>
    /// Inserimento massivo degli slot (es. pianificazione settimanale del medico).
    /// </summary>
    Task<bool> CreateBulkAsync(IEnumerable<SlotDisponibilita> slotList);

    /// <summary>
    /// Aggiorna lo stato di prenotazione dello slot (processo di prenotazione).
    /// </summary>
    Task<bool> UpdateStatoPrenotazioneAsync(Guid id, bool isPrenotato);

    /// <summary>
    /// Elimina uno slot solo se non è ancora stato prenotato.
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}
