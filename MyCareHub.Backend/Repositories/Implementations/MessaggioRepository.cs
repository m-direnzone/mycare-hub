using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Repositories.Interfaces;
using static Supabase.Postgrest.Constants;

namespace MyCareHub.Backend.Repositories.Implementations;

public class MessaggioRepository : IMessaggioRepository
{
    private readonly Supabase.Client _supabase;

    public MessaggioRepository(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<IEnumerable<Messaggio>> GetByConversazioneIdAsync(
        Guid conversazioneId,
        DateTimeOffset? after = null,
        int limit = 100)
    {
        var query = _supabase
            .From<Messaggio>()
            .Filter(x => x.ConversazioneId!, Operator.Equals, conversazioneId.ToString());

        var response = await query
            .Order(x => x.CreatoAt!, Ordering.Ascending)
            .Get();

        IEnumerable<Messaggio> models = response.Models;

        if (after.HasValue)
            models = models.Where(m => m.CreatoAt > after.Value);

        return models.TakeLast(Math.Clamp(limit, 1, 500)).ToList();
    }

    public async Task<Messaggio?> GetUltimoByConversazioneIdAsync(Guid conversazioneId)
    {
        var response = await _supabase
            .From<Messaggio>()
            .Filter(x => x.ConversazioneId!, Operator.Equals, conversazioneId.ToString())
            .Order(x => x.CreatoAt!, Ordering.Descending)
            .Limit(1)
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<Messaggio?> CreateAsync(Messaggio messaggio)
    {
        var now = DateTimeOffset.UtcNow;

        if (messaggio.Id == Guid.Empty)
            messaggio.Id = Guid.NewGuid();

        if (messaggio.CreatoAt == default)
            messaggio.CreatoAt = now;

        var response = await _supabase
            .From<Messaggio>()
            .Insert(messaggio);

        return response.Models.FirstOrDefault();
    }

    public async Task<int> CountNonLettiAsync(Guid conversazioneId, Guid esclusoMittenteId)
    {
        var response = await _supabase
            .From<Messaggio>()
            .Filter(x => x.ConversazioneId!, Operator.Equals, conversazioneId.ToString())
            .Filter(x => x.MittenteId!, Operator.NotEqual, esclusoMittenteId.ToString())
            .Get();

        return response.Models.Count(m => m.LettoAt is null);
    }

    public async Task<bool> SegnaLettiAsync(Guid conversazioneId, Guid lettoreId, DateTimeOffset lettoAt)
    {
        var response = await _supabase
            .From<Messaggio>()
            .Filter(x => x.ConversazioneId!, Operator.Equals, conversazioneId.ToString())
            .Filter(x => x.MittenteId!, Operator.NotEqual, lettoreId.ToString())
            .Get();

        var daSegnare = response.Models.Where(m => m.LettoAt is null).ToList();
        if (daSegnare.Count == 0)
            return true;

        foreach (var messaggio in daSegnare)
        {
            await _supabase
                .From<Messaggio>()
                .Filter(x => x.Id!, Operator.Equals, messaggio.Id.ToString())
                .Set(x => x.LettoAt!, lettoAt)
                .Update();
        }

        return true;
    }
}
