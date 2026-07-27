using MyCareHub.Backend.Entities;

namespace MyCareHub.Backend.Repositories.Interfaces;

public interface IProfiloRepository
{
    Task<Profilo?> GetByIdAsync(Guid id);
    Task<Profilo?> GetByCodiceFiscaleAsync(string codiceFiscale);
    Task<bool> UpdateAsync(Profilo profilo);
}
