using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Models.DTOs.Auth;
using MyCareHub.Backend.Models.DTOs.Profilo;
using MyCareHub.Backend.Repositories.Interfaces;
using MyCareHub.Backend.Services.Interfaces;

namespace MyCareHub.Backend.Services.Implementations;

public class ProfiloService : IProfiloService
{
    private readonly IProfiloRepository _profiloRepository;
    private readonly IMedicoRepository _medicoRepository;

    public ProfiloService(
        IProfiloRepository profiloRepository,
        IMedicoRepository medicoRepository)
    {
        _profiloRepository = profiloRepository;
        _medicoRepository = medicoRepository;
    }

    public async Task<ProfiloDto?> GetProfiloCompletoAsync(Guid utenteId)
    {
        var profilo = await _profiloRepository.GetByIdAsync(utenteId);
        if (profilo is null)
            return null;

        MedicoDettaglioDto? dettagliMedico = null;
        if (profilo.Ruolo == RuoloUtente.Medico)
        {
            var dettagli = await _medicoRepository.GetDettagliByIdAsync(utenteId);
            if (dettagli is not null)
                dettagliMedico = MapDettagli(dettagli);
        }

        return MapProfilo(profilo, dettagliMedico);
    }

    public async Task<bool> CompletaOnboardingMedicoAsync(Guid medicoId, MedicoOnboardingDto dto)
    {
        var profilo = await _profiloRepository.GetByIdAsync(medicoId);
        if (profilo is null || profilo.Ruolo != RuoloUtente.Medico)
            return false;

        var esistenti = await _medicoRepository.GetDettagliByIdAsync(medicoId);
        if (esistenti is not null)
            return false;

        profilo.Nome = dto.Nome.Trim();
        profilo.Cognome = dto.Cognome.Trim();
        profilo.CodiceFiscale = dto.CodiceFiscale.Trim().ToUpperInvariant();
        profilo.DataNascita = dto.DataNascita;
        profilo.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();

        var profiloAggiornato = await _profiloRepository.UpdateAsync(profilo);
        if (!profiloAggiornato)
            return false;

        var dettaglio = new MedicoDettaglio
        {
            MedicoId = medicoId,
            Specializzazione = dto.Specializzazione.Trim(),
            NumeroIscrizioneAlbo = dto.NumeroIscrizioneAlbo.Trim(),
            Biografia = string.IsNullOrWhiteSpace(dto.Biografia) ? null : dto.Biografia.Trim(),
            TariffaVisita = dto.TariffaVisita,
            Attivo = true
        };

        return await _medicoRepository.CreateMedicoDettagliAsync(dettaglio);
    }

    public async Task<bool> AggiornaProfiloAsync(Guid utenteId, ProfiloUpdateDto dto)
    {
        var profilo = await _profiloRepository.GetByIdAsync(utenteId);
        if (profilo is null)
            return false;

        profilo.Nome = dto.Nome.Trim();
        profilo.Cognome = dto.Cognome.Trim();
        profilo.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();

        return await _profiloRepository.UpdateAsync(profilo);
    }

    public async Task<bool> AggiornaDettagliMedicoAsync(Guid medicoId, MedicoDettaglioUpdateDto dto)
    {
        var profilo = await _profiloRepository.GetByIdAsync(medicoId);
        if (profilo is null || profilo.Ruolo != RuoloUtente.Medico)
            return false;

        var dettagli = await _medicoRepository.GetDettagliByIdAsync(medicoId);
        if (dettagli is null)
            return false;

        dettagli.Specializzazione = dto.Specializzazione.Trim();
        dettagli.Biografia = string.IsNullOrWhiteSpace(dto.Biografia) ? null : dto.Biografia.Trim();
        dettagli.TariffaVisita = dto.TariffaVisita;
        dettagli.Attivo = dto.Attivo;

        return await _medicoRepository.UpdateMedicoDettagliAsync(dettagli);
    }

    public async Task<IEnumerable<ProfiloDto>> GetMediciBySpecializzazioneAsync(string specializzazione)
    {
        if (string.IsNullOrWhiteSpace(specializzazione))
            return [];

        var medici = (await _medicoRepository.GetMediciBySpecializzazioneAsync(specializzazione.Trim())).ToList();
        if (medici.Count == 0)
            return [];

        var result = new List<ProfiloDto>(medici.Count);
        foreach (var medico in medici)
        {
            var dettagli = await _medicoRepository.GetDettagliByIdAsync(medico.Id);
            result.Add(MapProfilo(medico, dettagli is null ? null : MapDettagli(dettagli)));
        }

        return result;
    }

    private static ProfiloDto MapProfilo(Profilo profilo, MedicoDettaglioDto? dettagliMedico) => new()
    {
        Id = profilo.Id,
        Nome = profilo.Nome,
        Cognome = profilo.Cognome,
        CodiceFiscale = profilo.CodiceFiscale,
        DataNascita = profilo.DataNascita,
        Telefono = profilo.Telefono,
        Ruolo = profilo.Ruolo,
        DettagliMedico = dettagliMedico
    };

    private static MedicoDettaglioDto MapDettagli(MedicoDettaglio dettagli) => new()
    {
        Specializzazione = dettagli.Specializzazione,
        NumeroIscrizioneAlbo = dettagli.NumeroIscrizioneAlbo,
        Biografia = dettagli.Biografia,
        TariffaVisita = dettagli.TariffaVisita,
        Attivo = dettagli.Attivo
    };
}
