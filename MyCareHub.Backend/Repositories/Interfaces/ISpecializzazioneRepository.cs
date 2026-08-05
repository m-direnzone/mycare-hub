using MyCareHub.Backend.Entities;

namespace MyCareHub.Backend.Repositories.Interfaces;

public interface ISpecializzazioneRepository
{
    Task<IEnumerable<Specializzazione>> GetAllAsync();
    Task<Specializzazione?> GetByIdAsync(Guid id);
    Task<Specializzazione?> GetByNomeAsync(string nome);
    Task<Specializzazione?> GetByNomeIncludingDeletedAsync(string nome);
    Task<Specializzazione?> CreateAsync(Specializzazione entity);
    Task<bool> SoftDeleteAsync(Guid id);
    Task<bool> RestoreAsync(Guid id);
}
