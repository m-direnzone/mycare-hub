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
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
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
        var query = _supabase
            .From<Profilo>()
            .Filter(x => x.Id!, Operator.Equals, profilo.Id.ToString())
            .Set(x => x.Nome!, profilo.Nome)
            .Set(x => x.Cognome!, profilo.Cognome)
            .Set(x => x.CodiceFiscale!, profilo.CodiceFiscale)
            .Set(x => x.DataNascita!, profilo.DataNascita)
            .Set(x => x.AggiornatoAt!, DateTimeOffset.UtcNow);

        // Postgrest .Set non gestisce bene null su string?: usare stringa vuota e normalizzare a DB.
        query = query.Set(x => x.Telefono!, profilo.Telefono ?? string.Empty);

        var response = await query.Update();
        return response.Models.Count > 0;
    }

    public async Task<bool> UpdateRuoloAsync(Guid id, RuoloUtente ruolo)
    {
        var response = await _supabase
            .From<Profilo>()
            .Filter(x => x.Id!, Operator.Equals, id.ToString())
            .Set(x => x.Ruolo!, ruolo)
            .Set(x => x.AggiornatoAt!, DateTimeOffset.UtcNow)
            .Update();

        return response.Models.Count > 0;
    }

    public async Task<IEnumerable<Profilo>> GetByRuoloAsync(RuoloUtente ruolo)
    {
        var ruoloValue = ruolo switch
        {
            RuoloUtente.Paziente => "paziente",
            RuoloUtente.Medico => "medico",
            RuoloUtente.Admin => "admin",
            _ => ruolo.ToString().ToLowerInvariant()
        };

        var response = await _supabase
            .From<Profilo>()
            .Filter(x => x.Ruolo!, Operator.Equals, ruoloValue)
            .Order(x => x.Cognome!, Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<Profilo>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idList = ids.Select(id => id.ToString()).Distinct().ToList();
        if (idList.Count == 0)
            return [];

        var response = await _supabase
            .From<Profilo>()
            .Filter(x => x.Id!, Operator.In, idList)
            .Get();

        return response.Models;
    }
}
