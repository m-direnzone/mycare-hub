using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Repositories.Interfaces;
using static Supabase.Postgrest.Constants;

namespace MyCareHub.Backend.Repositories.Implementations;

public class ProfiloRepository : IProfiloRepository
{
    private readonly Supabase.Client _supabase;

    public ProfiloRepository(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<Profilo?> GetByIdAsync(Guid id)
    {
        var response = await _supabase
            .From<Profilo>()
            .Filter(x => x.Id!, Operator.Equals, id)
            .Limit(1)
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<Profilo?> GetByCodiceFiscaleAsync(string codiceFiscale)
    {
        var response = await _supabase
            .From<Profilo>()
            .Filter(x => x.CodiceFiscale!, Operator.Equals, codiceFiscale)
            .Limit(1)
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<bool> UpdateAsync(Profilo profilo)
    {
        // Il ruolo non è aggiornabile da qui: assegnazione solo via UpdateRuoloAsync / Auth.
        var response = await _supabase
            .From<Profilo>()
            .Filter(x => x.Id!, Operator.Equals, profilo.Id)
            .Set(x => x.Nome!, profilo.Nome)
            .Set(x => x.Cognome!, profilo.Cognome)
            .Set(x => x.CodiceFiscale!, profilo.CodiceFiscale)
            .Set(x => x.DataNascita!, profilo.DataNascita)
            .Set(x => x.Telefono!, profilo.Telefono)
            .Set(x => x.AggiornatoAt!, DateTimeOffset.UtcNow)
            .Update();

        return response.Models.Count > 0;
    }

    public async Task<bool> UpdateRuoloAsync(Guid id, RuoloUtente ruolo)
    {
        var response = await _supabase
            .From<Profilo>()
            .Filter(x => x.Id!, Operator.Equals, id)
            .Set(x => x.Ruolo!, ruolo)
            .Set(x => x.AggiornatoAt!, DateTimeOffset.UtcNow)
            .Update();

        return response.Models.Count > 0;
    }
}
