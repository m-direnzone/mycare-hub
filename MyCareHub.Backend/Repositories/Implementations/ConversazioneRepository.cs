using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Repositories.Interfaces;
using static Supabase.Postgrest.Constants;

namespace MyCareHub.Backend.Repositories.Implementations;

public class ConversazioneRepository : IConversazioneRepository
{
    private readonly Supabase.Client _supabase;

    public ConversazioneRepository(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<Conversazione?> GetByIdAsync(Guid id)
    {
        var response = await _supabase
            .From<Conversazione>()
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
            .Limit(1)
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<Conversazione?> GetByPartecipantiAsync(Guid pazienteId, Guid medicoId)
    {
        var response = await _supabase
            .From<Conversazione>()
            .Filter(x => x.PazienteId!, Operator.Equals, pazienteId.ToString())
            .Filter(x => x.MedicoId!, Operator.Equals, medicoId.ToString())
            .Limit(1)
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<IEnumerable<Conversazione>> GetByPazienteIdAsync(Guid pazienteId)
    {
        var response = await _supabase
            .From<Conversazione>()
            .Filter(x => x.PazienteId!, Operator.Equals, pazienteId.ToString())
            .Order(x => x.UltimoMessaggioAt!, Ordering.Descending)
            .Get();

        return response.Models
            .OrderByDescending(c => c.UltimoMessaggioAt ?? c.CreatoAt)
            .ToList();
    }

    public async Task<IEnumerable<Conversazione>> GetByMedicoIdAsync(Guid medicoId)
    {
        var response = await _supabase
            .From<Conversazione>()
            .Filter(x => x.MedicoId!, Operator.Equals, medicoId.ToString())
            .Order(x => x.UltimoMessaggioAt!, Ordering.Descending)
            .Get();

        return response.Models
            .OrderByDescending(c => c.UltimoMessaggioAt ?? c.CreatoAt)
            .ToList();
    }

    public async Task<Conversazione?> CreateAsync(Conversazione conversazione)
    {
        var now = DateTimeOffset.UtcNow;

        if (conversazione.Id == Guid.Empty)
            conversazione.Id = Guid.NewGuid();

        if (conversazione.CreatoAt == default)
            conversazione.CreatoAt = now;

        conversazione.AggiornatoAt = now;

        var response = await _supabase
            .From<Conversazione>()
            .Insert(conversazione);

        return response.Models.FirstOrDefault();
    }

    public async Task<bool> TouchUltimoMessaggioAsync(Guid id, DateTimeOffset at)
    {
        var response = await _supabase
            .From<Conversazione>()
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
            .Set(x => x.UltimoMessaggioAt!, at)
            .Set(x => x.AggiornatoAt!, at)
            .Update();

        return response.Models.Count > 0;
    }
}
