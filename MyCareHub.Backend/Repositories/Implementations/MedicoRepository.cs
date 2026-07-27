using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Repositories.Interfaces;
using static Supabase.Postgrest.Constants;

namespace MyCareHub.Backend.Repositories.Implementations;

public class MedicoRepository : IMedicoRepository
{
    private readonly Supabase.Client _supabase;

    public MedicoRepository(Supabase.Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<bool> CreateMedicoDettagliAsync(MedicoDettaglio dettaglio)
    {
        var response = await _supabase
            .From<MedicoDettaglio>()
            .Insert(dettaglio);

        return response.Models.Count > 0;
    }

    public async Task<MedicoDettaglio?> GetDettagliByIdAsync(Guid medicoId)
    {
        var response = await _supabase
            .From<MedicoDettaglio>()
            .Filter(x => x.MedicoId!, Operator.Equals, medicoId)
            .Limit(1)
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<bool> UpdateMedicoDettagliAsync(MedicoDettaglio dettaglio)
    {
        var response = await _supabase
            .From<MedicoDettaglio>()
            .Filter(x => x.MedicoId!, Operator.Equals, dettaglio.MedicoId)
            .Set(x => x.Specializzazione!, dettaglio.Specializzazione)
            .Set(x => x.NumeroIscrizioneAlbo!, dettaglio.NumeroIscrizioneAlbo)
            .Set(x => x.Biografia!, dettaglio.Biografia)
            .Set(x => x.TariffaVisita!, dettaglio.TariffaVisita)
            .Set(x => x.Attivo!, dettaglio.Attivo)
            .Update();

        return response.Models.Count > 0;
    }

    public async Task<IEnumerable<Profilo>> GetMediciBySpecializzazioneAsync(string specializzazione)
    {
        var dettagliResponse = await _supabase
            .From<MedicoDettaglio>()
            .Filter(x => x.Specializzazione!, Operator.ILike, specializzazione)
            .Filter(x => x.Attivo!, Operator.Equals, true)
            .Get();

        var medicoIds = dettagliResponse.Models
            .Select(d => d.MedicoId)
            .Distinct()
            .ToList();

        if (medicoIds.Count == 0)
            return [];

        var profiliResponse = await _supabase
            .From<Profilo>()
            .Filter(x => x.Id!, Operator.In, medicoIds)
            .Filter(x => x.Ruolo!, Operator.Equals, RuoloUtente.Medico)
            .Get();

        return profiliResponse.Models;
    }

    public async Task<IEnumerable<MedicoDettaglio>> GetAllAttiviAsync()
    {
        var response = await _supabase
            .From<MedicoDettaglio>()
            .Filter(x => x.Attivo!, Operator.Equals, true)
            .Get();

        return response.Models;
    }
}
