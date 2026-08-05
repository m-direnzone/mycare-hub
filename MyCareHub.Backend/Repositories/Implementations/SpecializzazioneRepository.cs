using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Repositories.Interfaces;
using static Supabase.Postgrest.Constants;

namespace MyCareHub.Backend.Repositories.Implementations;

public class SpecializzazioneRepository : ISpecializzazioneRepository
{
    private readonly Supabase.Client _supabase;

    public SpecializzazioneRepository(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<IEnumerable<Specializzazione>> GetAllAsync()
    {
        var response = await _supabase
            .From<Specializzazione>()
            .Filter(x => x.DeletedAt!, Operator.Is, "null")
            .Order(x => x.Nome!, Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<Specializzazione?> GetByIdAsync(Guid id)
    {
        var response = await _supabase
            .From<Specializzazione>()
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
            .Limit(1)
            .Get();

        var entity = response.Models.FirstOrDefault();
        return entity is { DeletedAt: null } ? entity : null;
    }

    public async Task<Specializzazione?> GetByNomeAsync(string nome)
    {
        var response = await _supabase
            .From<Specializzazione>()
            .Filter(x => x.Nome!, Operator.Equals, nome.Trim())
            .Limit(1)
            .Get();

        var entity = response.Models.FirstOrDefault();
        return entity is { DeletedAt: null } ? entity : null;
    }

    public async Task<Specializzazione?> GetByNomeIncludingDeletedAsync(string nome)
    {
        var response = await _supabase
            .From<Specializzazione>()
            .Filter(x => x.Nome!, Operator.Equals, nome.Trim())
            .Order(x => x.CreatoAt!, Ordering.Descending)
            .Limit(1)
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<Specializzazione?> CreateAsync(Specializzazione entity)
    {
        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();
        if (entity.CreatoAt == default)
            entity.CreatoAt = DateTimeOffset.UtcNow;

        var response = await _supabase
            .From<Specializzazione>()
            .Insert(entity);

        return response.Models.FirstOrDefault();
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        var response = await _supabase
            .From<Specializzazione>()
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
            .Set(x => x.DeletedAt!, DateTimeOffset.UtcNow)
            .Update();

        return response.Models.Count > 0;
    }

    public async Task<bool> RestoreAsync(Guid id)
    {
        var response = await _supabase
            .From<Specializzazione>()
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
            .Set(x => x.DeletedAt!, (DateTimeOffset?)null)
            .Update();

        return response.Models.Count > 0;
    }
}
