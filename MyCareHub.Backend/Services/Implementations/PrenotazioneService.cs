using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Models.DTOs.Prenotazione;
using MyCareHub.Backend.Repositories.Interfaces;
using MyCareHub.Backend.Services.Interfaces;

namespace MyCareHub.Backend.Services.Implementations;

public class PrenotazioneService : IPrenotazioneService
{
    private static readonly HashSet<(StatoPrenotazione From, StatoPrenotazione To)> TransizioniConsentite =
    [
        (StatoPrenotazione.InAttesa, StatoPrenotazione.Confermata),
        (StatoPrenotazione.InAttesa, StatoPrenotazione.Cancellata),
        (StatoPrenotazione.Confermata, StatoPrenotazione.Cancellata),
        (StatoPrenotazione.Confermata, StatoPrenotazione.Completata)
    ];

    private readonly IPrenotazioneRepository _prenotazioneRepository;
    private readonly ISlotDisponibilitaRepository _slotRepository;
    private readonly IProfiloRepository _profiloRepository;
    private readonly IMedicoRepository _medicoRepository;

    public PrenotazioneService(
        IPrenotazioneRepository prenotazioneRepository,
        ISlotDisponibilitaRepository slotRepository,
        IProfiloRepository profiloRepository,
        IMedicoRepository medicoRepository)
    {
        _prenotazioneRepository = prenotazioneRepository;
        _slotRepository = slotRepository;
        _profiloRepository = profiloRepository;
        _medicoRepository = medicoRepository;
    }

    public async Task<PrenotazioneDto?> PrenotaVisitaAsync(Guid pazienteId, PrenotazioneCreateDto dto)
    {
        var paziente = await _profiloRepository.GetByIdAsync(pazienteId);
        if (paziente is null || paziente.Ruolo != RuoloUtente.Paziente)
            return null;

        var slot = await _slotRepository.GetByIdAsync(dto.SlotId);
        if (slot is null || slot.IsPrenotato || slot.MedicoId != dto.MedicoId)
            return null;

        if (slot.DataOraInizio < DateTimeOffset.UtcNow)
            return null;

        var medico = await _profiloRepository.GetByIdAsync(dto.MedicoId);
        if (medico is null || medico.Ruolo != RuoloUtente.Medico)
            return null;

        var dettagliMedico = await _medicoRepository.GetDettagliByIdAsync(dto.MedicoId);
        if (dettagliMedico is null || !dettagliMedico.Attivo)
            return null;

        // Lock ottimistico: solo il primo richiedente riesce a passare is_prenotato da false a true
        var slotBloccato = await _slotRepository.TryLockSlotAsync(dto.SlotId);
        if (!slotBloccato)
            return null;

        var now = DateTimeOffset.UtcNow;
        var prenotazione = new Prenotazione
        {
            Id = Guid.NewGuid(),
            PazienteId = pazienteId,
            MedicoId = dto.MedicoId,
            SlotId = dto.SlotId,
            Stato = StatoPrenotazione.InAttesa,
            NotePaziente = string.IsNullOrWhiteSpace(dto.NotePaziente) ? null : dto.NotePaziente.Trim(),
            CreatoAt = now,
            AggiornatoAt = now
        };

        var creata = await _prenotazioneRepository.CreateAsync(prenotazione);
        if (creata is null)
        {
            // Compensazione: libera lo slot se l'inserimento fallisce
            await _slotRepository.UpdateStatoPrenotazioneAsync(dto.SlotId, isPrenotato: false);
            return null;
        }

        return MapPrenotazione(creata, slot, medico, dettagliMedico, paziente);
    }

    public async Task<IEnumerable<PrenotazioneDto>> GetVisitePazienteAsync(Guid pazienteId)
    {
        var prenotazioni = (await _prenotazioneRepository.GetByPazienteIdAsync(pazienteId)).ToList();
        return await MapManyAsync(prenotazioni);
    }

    public async Task<IEnumerable<PrenotazioneDto>> GetVisiteMedicoAsync(
        Guid medicoId,
        DateTimeOffset? dataInizio = null,
        DateTimeOffset? dataFine = null)
    {
        var prenotazioni = (await _prenotazioneRepository.GetByMedicoIdAsync(medicoId, dataInizio, dataFine)).ToList();
        return await MapManyAsync(prenotazioni);
    }

    public async Task<PrenotazioneDto?> GetVisitaByIdAsync(Guid prenotazioneId)
    {
        var prenotazione = await _prenotazioneRepository.GetByIdAsync(prenotazioneId);
        if (prenotazione is null)
            return null;

        var mapped = await MapManyAsync([prenotazione]);
        return mapped.FirstOrDefault();
    }

    public async Task<bool> CambiaStatoVisitaAsync(Guid prenotazioneId, StatoPrenotazione nuovoStato)
    {
        var prenotazione = await _prenotazioneRepository.GetByIdAsync(prenotazioneId);
        if (prenotazione is null)
            return false;

        if (prenotazione.Stato == nuovoStato)
            return true;

        if (!TransizioniConsentite.Contains((prenotazione.Stato, nuovoStato)))
            return false;

        // Completata solo tramite ConcludiVisitaAsync (con note cliniche obbligatorie)
        if (nuovoStato == StatoPrenotazione.Completata)
            return false;

        var aggiornato = await _prenotazioneRepository.UpdateStatoAsync(prenotazioneId, nuovoStato);
        if (!aggiornato)
            return false;

        if (nuovoStato == StatoPrenotazione.Cancellata)
            await _slotRepository.UpdateStatoPrenotazioneAsync(prenotazione.SlotId, isPrenotato: false);

        return true;
    }

    public async Task<bool> ConcludiVisitaAsync(Guid prenotazioneId, Guid medicoId, PrenotazioneConclusioneDto dto)
    {
        var prenotazione = await _prenotazioneRepository.GetByIdAsync(prenotazioneId);
        if (prenotazione is null || prenotazione.MedicoId != medicoId)
            return false;

        if (prenotazione.Stato is not (StatoPrenotazione.Confermata or StatoPrenotazione.InAttesa))
            return false;

        var noteAggiornate = await _prenotazioneRepository.UpdateNoteMedicheAsync(
            prenotazioneId,
            dto.NoteMediche.Trim());

        if (!noteAggiornate)
            return false;

        return await _prenotazioneRepository.UpdateStatoAsync(prenotazioneId, StatoPrenotazione.Completata);
    }

    private async Task<IEnumerable<PrenotazioneDto>> MapManyAsync(IReadOnlyList<Prenotazione> prenotazioni)
    {
        if (prenotazioni.Count == 0)
            return [];

        var result = new List<PrenotazioneDto>(prenotazioni.Count);

        foreach (var prenotazione in prenotazioni)
        {
            var slot = await _slotRepository.GetByIdAsync(prenotazione.SlotId);
            var medico = await _profiloRepository.GetByIdAsync(prenotazione.MedicoId);
            var paziente = await _profiloRepository.GetByIdAsync(prenotazione.PazienteId);
            var dettagliMedico = await _medicoRepository.GetDettagliByIdAsync(prenotazione.MedicoId);

            if (slot is null || medico is null || paziente is null)
                continue;

            result.Add(MapPrenotazione(prenotazione, slot, medico, dettagliMedico, paziente));
        }

        return result;
    }

    private static PrenotazioneDto MapPrenotazione(
        Prenotazione prenotazione,
        SlotDisponibilita slot,
        Profilo medico,
        MedicoDettaglio? dettagliMedico,
        Profilo paziente) => new()
    {
        Id = prenotazione.Id,
        SlotId = prenotazione.SlotId,
        Stato = prenotazione.Stato,
        NotePaziente = prenotazione.NotePaziente,
        NoteMediche = prenotazione.NoteMediche,
        CreatoAt = prenotazione.CreatoAt,
        AggiornatoAt = prenotazione.AggiornatoAt,
        DataOraInizio = slot.DataOraInizio,
        DataOraFine = slot.DataOraFine,
        MedicoId = prenotazione.MedicoId,
        MedicoNomeCompleto = $"{medico.Nome} {medico.Cognome}",
        MedicoSpecializzazione = dettagliMedico?.Specializzazione ?? string.Empty,
        PazienteId = prenotazione.PazienteId,
        PazienteNomeCompleto = $"{paziente.Nome} {paziente.Cognome}",
        PazienteCodiceFiscale = paziente.CodiceFiscale
    };
}
