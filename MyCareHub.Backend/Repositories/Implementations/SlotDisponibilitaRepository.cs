using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Repositories.Interfaces;
using static Supabase.Postgrest.Constants;

namespace MyCareHub.Backend.Repositories.Implementations;

public class SlotDisponibilitaRepository : ISlotDisponibilitaRepository
{
    private readonly Supabase.Client _supabase;

    public SlotDisponibilitaRepository(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<SlotDisponibilita?> GetByIdAsync(Guid id)
    {
        var response = await _supabase
            .From<SlotDisponibilita>()
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
            .Limit(1)
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<IEnumerable<SlotDisponibilita>> GetByMedicoIdAsync(
        Guid medicoId,
        DateTimeOffset? dataInizio = null,
        DateTimeOffset? dataFine = null,
        bool? isPrenotato = null)
    {
        var query = _supabase
            .From<SlotDisponibilita>()
            .Filter(x => x.MedicoId!, Operator.Equals, medicoId.ToString());

        if (dataInizio.HasValue)
            query = query.Filter(x => x.DataOraInizio!, Operator.GreaterThanOrEqual, dataInizio.Value);

        if (dataFine.HasValue)
            query = query.Filter(x => x.DataOraInizio!, Operator.LessThanOrEqual, dataFine.Value);

        if (isPrenotato.HasValue)
            query = query.Filter(x => x.IsPrenotato!, Operator.Equals, isPrenotato.Value ? "true" : "false");

        var response = await query
            .Order(x => x.DataOraInizio!, Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<SlotDisponibilita?> CreateAsync(SlotDisponibilita slot)
    {
        var now = DateTimeOffset.UtcNow;

        if (slot.Id == Guid.Empty)
            slot.Id = Guid.NewGuid();

        if (slot.CreatoAt == default)
            slot.CreatoAt = now;

        var response = await _supabase
            .From<SlotDisponibilita>()
            .Insert(slot);

        var created = response.Models.FirstOrDefault();
        if (created is not null)
            return created;

        // Fallback: INSERT ok ma RETURNING filtrato da RLS → rileggi per chiave naturale.
        var esistenti = await GetByMedicoIdAsync(
            slot.MedicoId,
            slot.DataOraInizio.AddMinutes(-1),
            slot.DataOraInizio.AddMinutes(1));

        return esistenti.FirstOrDefault(s =>
            s.DataOraInizio.UtcTicks == slot.DataOraInizio.UtcTicks &&
            s.DataOraFine.UtcTicks == slot.DataOraFine.UtcTicks);
    }

    public async Task<bool> CreateBulkAsync(IEnumerable<SlotDisponibilita> slotList)
    {
        var slots = slotList.ToList();
        if (slots.Count == 0)
            return false;

        var now = DateTimeOffset.UtcNow;
        foreach (var slot in slots)
        {
            if (slot.Id == Guid.Empty)
                slot.Id = Guid.NewGuid();

            if (slot.CreatoAt == default)
                slot.CreatoAt = now;
        }

        var response = await _supabase
            .From<SlotDisponibilita>()
            .Insert(slots);

        return response.Models.Count > 0;
    }

    public async Task<bool> UpdateStatoPrenotazioneAsync(Guid id, bool isPrenotato)
    {
        var response = await _supabase
            .From<SlotDisponibilita>()
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
            .Set(x => x.IsPrenotato!, isPrenotato)
            .Update();

        return response.Models.Count > 0;
    }

    public async Task<bool> TryLockSlotAsync(Guid id)
    {
        var response = await _supabase
            .From<SlotDisponibilita>()
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
            .Filter(x => x.IsPrenotato!, Operator.Equals, "false")
            .Set(x => x.IsPrenotato!, true)
            .Update();

        return response.Models.Count > 0;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var slot = await GetByIdAsync(id);
        if (slot is null || slot.IsPrenotato)
            return false;

        await _supabase
            .From<SlotDisponibilita>()
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
            .Delete();

        return true;
    }

    public async Task<IEnumerable<SlotDisponibilita>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Select(id => id.ToString()).Distinct().ToList();
        if (idList.Count == 0)
            return [];

        var response = await _supabase
            .From<SlotDisponibilita>()
            .Filter(x => x.Id!, Operator.In, idList)
            .Get();

        return response.Models;
    }
}
