using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Repositories.Interfaces;
using static Supabase.Postgrest.Constants;

namespace MyCareHub.Backend.Repositories.Implementations;

public class PrenotazioneRepository : IPrenotazioneRepository
{
    private readonly Supabase.Client _supabase;

    public PrenotazioneRepository(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<Prenotazione?> GetByIdAsync(Guid id)
    {
        var response = await _supabase
            .From<Prenotazione>()
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
            .Limit(1)
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<IEnumerable<Prenotazione>> GetByPazienteIdAsync(Guid pazienteId)
    {
        var response = await _supabase
            .From<Prenotazione>()
            .Filter(x => x.PazienteId!, Operator.Equals, pazienteId.ToString())
            .Order(x => x.CreatoAt!, Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<Prenotazione>> GetByMedicoIdAsync(
        Guid medicoId,
        DateTimeOffset? dataInizio = null,
        DateTimeOffset? dataFine = null)
    {
        if (dataInizio is null && dataFine is null)
        {
            var response = await _supabase
                .From<Prenotazione>()
                .Filter(x => x.MedicoId!, Operator.Equals, medicoId.ToString())
                .Order(x => x.CreatoAt!, Ordering.Ascending)
                .Get();

            return response.Models;
        }

        // Agenda giornaliera/settimanale: filtra slot/date in memoria
        // (evita Bad Request PostgREST su Filter DateTimeOffset e su IN troppo lunghi).
        var slotsResponse = await _supabase
            .From<SlotDisponibilita>()
            .Filter(x => x.MedicoId!, Operator.Equals, medicoId.ToString())
            .Get();

        var slotIds = slotsResponse.Models
            .Where(s =>
                (!dataInizio.HasValue || s.DataOraInizio >= dataInizio.Value) &&
                (!dataFine.HasValue || s.DataOraInizio <= dataFine.Value))
            .Select(s => s.Id)
            .ToHashSet();

        if (slotIds.Count == 0)
            return [];

        var prenotazioniResponse = await _supabase
            .From<Prenotazione>()
            .Filter(x => x.MedicoId!, Operator.Equals, medicoId.ToString())
            .Get();

        return prenotazioniResponse.Models
            .Where(p => slotIds.Contains(p.SlotId))
            .ToList();
    }

    public async Task<Prenotazione?> CreateAsync(Prenotazione prenotazione)
    {
        var now = DateTimeOffset.UtcNow;

        if (prenotazione.Id == Guid.Empty)
            prenotazione.Id = Guid.NewGuid();

        if (prenotazione.CreatoAt == default)
            prenotazione.CreatoAt = now;

        prenotazione.AggiornatoAt = now;

        var response = await _supabase
            .From<Prenotazione>()
            .Insert(prenotazione);

        return response.Models.FirstOrDefault();
    }

    public async Task<bool> UpdateStatoAsync(Guid id, StatoPrenotazione nuovoStato)
    {
        var response = await _supabase
            .From<Prenotazione>()
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
            .Set(x => x.Stato!, nuovoStato)
            .Set(x => x.AggiornatoAt!, DateTimeOffset.UtcNow)
            .Update();

        return response.Models.Count > 0;
    }

    public async Task<bool> UpdateNoteMedicheAsync(Guid id, string noteMediche)
    {
        var response = await _supabase
            .From<Prenotazione>()
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
            .Set(x => x.NoteMediche!, noteMediche)
            .Set(x => x.AggiornatoAt!, DateTimeOffset.UtcNow)
            .Update();

        return response.Models.Count > 0;
    }

    public async Task<IEnumerable<Prenotazione>> GetAllAsync(
        DateTimeOffset? dataInizio = null,
        DateTimeOffset? dataFine = null)
    {
        if (dataInizio is null && dataFine is null)
        {
            var response = await _supabase
                .From<Prenotazione>()
                .Order(x => x.CreatoAt!, Ordering.Descending)
                .Get();

            return response.Models;
        }

        // Carica tutti gli slot e filtra in memoria sul range (evita catene Filter tipizzate).
        var slotsResponse = await _supabase
            .From<SlotDisponibilita>()
            .Get();

        var slotIds = slotsResponse.Models
            .Where(s =>
                (!dataInizio.HasValue || s.DataOraInizio >= dataInizio.Value) &&
                (!dataFine.HasValue || s.DataOraInizio <= dataFine.Value))
            .Select(s => s.Id.ToString())
            .Distinct()
            .ToList();

        if (slotIds.Count == 0)
            return [];

        var prenotazioniResponse = await _supabase
            .From<Prenotazione>()
            .Filter(x => x.SlotId!, Operator.In, slotIds)
            .Get();

        return prenotazioniResponse.Models;
    }
}
