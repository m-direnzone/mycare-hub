using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Models.DTOs.Prenotazione;

namespace MyCareHub.Backend.Services.Interfaces;

public interface IPrenotazioneService
{
    /// <summary>
    /// Workflow transazionale: verifica lo slot, lo blocca e crea la prenotazione.
    /// </summary>
    Task<PrenotazioneDto?> PrenotaVisitaAsync(Guid pazienteId, PrenotazioneCreateDto dto);

    Task<IEnumerable<PrenotazioneDto>> GetVisitePazienteAsync(Guid pazienteId);

    Task<IEnumerable<PrenotazioneDto>> GetVisiteMedicoAsync(
        Guid medicoId,
        DateTimeOffset? dataInizio = null,
        DateTimeOffset? dataFine = null);

    Task<PrenotazioneDto?> GetVisitaByIdAsync(Guid prenotazioneId);

    /// <summary>
    /// Cambia lo stato della visita (es. Confermata, Cancellata).
    /// In caso di cancellazione libera lo slot associato.
    /// </summary>
    Task<bool> CambiaStatoVisitaAsync(Guid prenotazioneId, StatoPrenotazione nuovoStato);

    /// <summary>
    /// Il medico conclude la visita inserendo il resoconto clinico.
    /// </summary>
    Task<bool> ConcludiVisitaAsync(Guid prenotazioneId, Guid medicoId, PrenotazioneConclusioneDto dto);
}
