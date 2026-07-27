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
            .Filter(x => x.Id!, Operator.Equals, id)
            .Limit(1)
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<IEnumerable<Prenotazione>> GetByPazienteIdAsync(Guid pazienteId)
    {
        var response = await _supabase
            .From<Prenotazione>()
            .Filter(x => x.PazienteId!, Operator.Equals, pazienteId)
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
                .Filter(x => x.MedicoId!, Operator.Equals, medicoId)
                .Order(x => x.CreatoAt!, Ordering.Ascending)
                .Get();

            return response.Models;
        }

        // Agenda giornaliera/settimanale: filtra tramite gli slot nel range richiesto
        var slotQuery = _supabase
            .From<SlotDisponibilita>()
            .Filter(x => x.MedicoId!, Operator.Equals, medicoId);

        if (dataInizio.HasValue)
            slotQuery = slotQuery.Filter(x => x.DataOraInizio!, Operator.GreaterThanOrEqual, dataInizio.Value);

        if (dataFine.HasValue)
            slotQuery = slotQuery.Filter(x => x.DataOraInizio!, Operator.LessThanOrEqual, dataFine.Value);

        var slotsResponse = await slotQuery.Get();
        var slotIds = slotsResponse.Models
            .Select(s => s.Id)
            .Distinct()
            .ToList();

        if (slotIds.Count == 0)
            return [];

        var prenotazioniResponse = await _supabase
            .From<Prenotazione>()
            .Filter(x => x.MedicoId!, Operator.Equals, medicoId)
            .Filter(x => x.SlotId!, Operator.In, slotIds)
            .Get();

        return prenotazioniResponse.Models;
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
            .Filter(x => x.Id!, Operator.Equals, id)
            .Set(x => x.Stato!, nuovoStato)
            .Set(x => x.AggiornatoAt!, DateTimeOffset.UtcNow)
            .Update();

        return response.Models.Count > 0;
    }

    public async Task<bool> UpdateNoteMedicheAsync(Guid id, string noteMediche)
    {
        var response = await _supabase
            .From<Prenotazione>()
            .Filter(x => x.Id!, Operator.Equals, id)
            .Set(x => x.NoteMediche!, noteMediche)
            .Set(x => x.AggiornatoAt!, DateTimeOffset.UtcNow)
            .Update();

        return response.Models.Count > 0;
    }
}
