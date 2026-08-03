using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Repositories.Interfaces;
using static Supabase.Postgrest.Constants;

namespace MyCareHub.Backend.Repositories.Implementations;

public class RefertoMedicoRepository : IRefertoMedicoRepository
{
    private readonly Supabase.Client _supabase;

    public RefertoMedicoRepository(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<RefertoMedico?> GetByIdAsync(Guid id)
    {
        var response = await _supabase
            .From<RefertoMedico>()
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
            .Limit(1)
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<IEnumerable<RefertoMedico>> GetByPazienteIdAsync(Guid pazienteId)
    {
        var response = await _supabase
            .From<RefertoMedico>()
            .Filter(x => x.PazienteId!, Operator.Equals, pazienteId.ToString())
            .Order(x => x.DataEmissione!, Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<RefertoMedico>> GetByMedicoIdAsync(Guid medicoId)
    {
        var response = await _supabase
            .From<RefertoMedico>()
            .Filter(x => x.MedicoId!, Operator.Equals, medicoId.ToString())
            .Order(x => x.DataEmissione!, Ordering.Descending)
            .Get();

        return response.Models;
    }

    public async Task<RefertoMedico?> CreateAsync(RefertoMedico referto)
    {
        if (referto.Id == Guid.Empty)
            referto.Id = Guid.NewGuid();

        if (referto.CreatoAt == default)
            referto.CreatoAt = DateTimeOffset.UtcNow;

        var response = await _supabase
            .From<RefertoMedico>()
            .Insert(referto);

        return response.Models.FirstOrDefault();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await GetByIdAsync(id);
        if (existing is null)
            return false;

        await _supabase
            .From<RefertoMedico>()
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
            .Delete();

        return true;
    }
}
