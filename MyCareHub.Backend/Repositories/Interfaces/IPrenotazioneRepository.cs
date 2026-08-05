using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Entities.Enums;

namespace MyCareHub.Backend.Repositories.Interfaces;

public interface IPrenotazioneRepository
{
    Task<Prenotazione?> GetByIdAsync(Guid id);

    /// <summary>
    /// Storico prenotazioni di un paziente.
    /// </summary>
    Task<IEnumerable<Prenotazione>> GetByPazienteIdAsync(Guid pazienteId);

    /// <summary>
    /// Agenda prenotazioni di un medico, con filtri opzionali sul range temporale dello slot associato.
    /// </summary>
    Task<IEnumerable<Prenotazione>> GetByMedicoIdAsync(
        Guid medicoId,
        DateTimeOffset? dataInizio = null,
        DateTimeOffset? dataFine = null);

    Task<Prenotazione?> CreateAsync(Prenotazione prenotazione);

    Task<bool> UpdateStatoAsync(Guid id, StatoPrenotazione nuovoStato);

    Task<bool> UpdateNoteMedicheAsync(Guid id, string noteMediche);

    /// <summary>
    /// Elenco globale prenotazioni (admin), con filtro opzionale sullo slot temporale.
    /// </summary>
    Task<IEnumerable<Prenotazione>> GetAllAsync(
        DateTimeOffset? dataInizio = null,
        DateTimeOffset? dataFine = null);
}
