using MyCareHub.Backend.Entities;

namespace MyCareHub.Backend.Repositories.Interfaces;

public interface IMessaggioRepository
{
    Task<IEnumerable<Messaggio>> GetByConversazioneIdAsync(
        Guid conversazioneId,
        DateTimeOffset? after = null,
        int limit = 100);

    Task<Messaggio?> GetUltimoByConversazioneIdAsync(Guid conversazioneId);

    Task<Messaggio?> CreateAsync(Messaggio messaggio);

    Task<int> CountNonLettiAsync(Guid conversazioneId, Guid esclusoMittenteId);

    Task<bool> SegnaLettiAsync(Guid conversazioneId, Guid lettoreId, DateTimeOffset lettoAt);
}
