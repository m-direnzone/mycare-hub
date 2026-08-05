using MyCareHub.Backend.Entities;
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
            .Filter(x => x.MedicoId!, Operator.Equals, medicoId.ToString())
            .Limit(1)
            .Get();

        return response.Models.FirstOrDefault();
    }

    public async Task<bool> ExistsByNumeroAlboAsync(string numeroIscrizioneAlbo)
    {
        var response = await _supabase
            .From<MedicoDettaglio>()
            .Filter(x => x.NumeroIscrizioneAlbo!, Operator.Equals, numeroIscrizioneAlbo)
            .Limit(1)
            .Get();

        return response.Models.Count > 0;
    }

    public async Task<bool> UpdateMedicoDettagliAsync(MedicoDettaglio dettaglio)
    {
        var response = await _supabase
            .From<MedicoDettaglio>()
            .Filter(x => x.MedicoId!, Operator.Equals, dettaglio.MedicoId.ToString())
            .Set(x => x.Specializzazione!, dettaglio.Specializzazione)
            .Set(x => x.NumeroIscrizioneAlbo!, dettaglio.NumeroIscrizioneAlbo)
            .Set(x => x.Biografia!, dettaglio.Biografia)
            .Set(x => x.TariffaVisita!, dettaglio.TariffaVisita)
            .Set(x => x.Attivo!, dettaglio.Attivo)
            .Set(x => x.PasswordCambiata!, dettaglio.PasswordCambiata)
            .Update();

        return response.Models.Count > 0;
    }

    public async Task<bool> MarkPasswordCambiataAsync(Guid medicoId)
    {
        var response = await _supabase
            .From<MedicoDettaglio>()
            .Filter(x => x.MedicoId!, Operator.Equals, medicoId.ToString())
            .Set(x => x.PasswordCambiata!, true)
            .Update();

        return response.Models.Count > 0;
    }

    public async Task<IEnumerable<Profilo>> GetMediciBySpecializzazioneAsync(string specializzazione)
    {
        var risultati = await GetMediciAttiviAsync(specializzazione: specializzazione);
        return risultati.Select(r => r.Profilo);
    }

    public async Task<IEnumerable<(Profilo Profilo, MedicoDettaglio Dettagli)>> GetMediciAttiviAsync(
        string? specializzazione = null,
        string? nome = null)
    {
        // Postgrest non accetta bool/Guid/enum nei Filter: passare stringhe.
        var dettagliQuery = _supabase
            .From<MedicoDettaglio>()
            .Filter(x => x.Attivo!, Operator.Equals, "true");

        if (!string.IsNullOrWhiteSpace(specializzazione))
        {
            dettagliQuery = dettagliQuery.Filter(
                x => x.Specializzazione!,
                Operator.ILike,
                $"%{specializzazione.Trim()}%");
        }

        var dettagliResponse = await dettagliQuery.Get();
        var dettagli = dettagliResponse.Models;
        if (dettagli.Count == 0)
            return [];

        var medicoIds = dettagli
            .Select(d => d.MedicoId.ToString())
            .Distinct()
            .ToList();

        var profiliResponse = await _supabase
            .From<Profilo>()
            .Filter(x => x.Id!, Operator.In, medicoIds)
            .Filter(x => x.Ruolo!, Operator.Equals, "medico")
            .Get();

        var dettagliById = dettagli.ToDictionary(d => d.MedicoId);
        var risultati = new List<(Profilo, MedicoDettaglio)>();

        foreach (var profilo in profiliResponse.Models)
        {
            if (!dettagliById.TryGetValue(profilo.Id, out var dett))
                continue;

            if (!string.IsNullOrWhiteSpace(nome))
            {
                var needle = nome.Trim();
                var fullName = $"{profilo.Nome} {profilo.Cognome}";
                if (fullName.Contains(needle, StringComparison.OrdinalIgnoreCase) is false
                    && profilo.Nome.Contains(needle, StringComparison.OrdinalIgnoreCase) is false
                    && profilo.Cognome.Contains(needle, StringComparison.OrdinalIgnoreCase) is false)
                {
                    continue;
                }
            }

            risultati.Add((profilo, dett));
        }

        return risultati;
    }

    public async Task<IEnumerable<MedicoDettaglio>> GetAllAttiviAsync()
    {
        var response = await _supabase
            .From<MedicoDettaglio>()
            .Filter(x => x.Attivo!, Operator.Equals, "true")
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<MedicoDettaglio>> GetAllDettagliAsync()
    {
        var response = await _supabase
            .From<MedicoDettaglio>()
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<(Profilo Profilo, MedicoDettaglio Dettagli)>> GetTuttiMediciAsync()
    {
        var dettagliResponse = await _supabase
            .From<MedicoDettaglio>()
            .Get();

        var dettagli = dettagliResponse.Models;
        if (dettagli.Count == 0)
            return [];

        var medicoIds = dettagli
            .Select(d => d.MedicoId.ToString())
            .Distinct()
            .ToList();

        var profiliResponse = await _supabase
            .From<Profilo>()
            .Filter(x => x.Id!, Operator.In, medicoIds)
            .Filter(x => x.Ruolo!, Operator.Equals, "medico")
            .Get();

        var dettagliById = dettagli.ToDictionary(d => d.MedicoId);
        var risultati = new List<(Profilo, MedicoDettaglio)>();

        foreach (var profilo in profiliResponse.Models.OrderBy(p => p.Cognome).ThenBy(p => p.Nome))
        {
            if (!dettagliById.TryGetValue(profilo.Id, out var dett))
                continue;

            risultati.Add((profilo, dett));
        }

        return risultati;
    }
}
