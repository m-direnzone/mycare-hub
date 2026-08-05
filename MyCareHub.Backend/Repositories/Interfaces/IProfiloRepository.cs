using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Entities.Enums;

namespace MyCareHub.Backend.Repositories.Interfaces;

public interface IProfiloRepository
{
    Task<Profilo?> GetByIdAsync(Guid id);
    Task<Profilo?> GetByCodiceFiscaleAsync(string codiceFiscale);
    Task<bool> UpdateAsync(Profilo profilo);
    Task<bool> UpdateRuoloAsync(Guid id, RuoloUtente ruolo);
    Task<IEnumerable<Profilo>> GetByRuoloAsync(RuoloUtente ruolo);
    Task<IEnumerable<Profilo>> GetByIdsAsync(IEnumerable<Guid> ids);
}
