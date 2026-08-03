using System.Security.Cryptography;
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
    private readonly ISupabaseAuthAdminService _authAdminService;

    public ProfiloService(
        IProfiloRepository profiloRepository,
        IMedicoRepository medicoRepository,
        ISupabaseAuthAdminService authAdminService)
    {
        _profiloRepository = profiloRepository;
        _medicoRepository = medicoRepository;
        _authAdminService = authAdminService;
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

    public async Task<MedicoCreateResponseDto> CreaMedicoAsync(
        MedicoOnboardingDto dto,
        CancellationToken cancellationToken = default)
    {
        var cf = dto.CodiceFiscale.Trim().ToUpperInvariant();
        var esistenteCf = await _profiloRepository.GetByCodiceFiscaleAsync(cf);
        if (esistenteCf is not null)
            throw new InvalidOperationException("Codice fiscale già registrato nel sistema.");

        var albo = dto.NumeroIscrizioneAlbo.Trim();
        var alboOccupato = await _medicoRepository.ExistsByNumeroAlboAsync(albo);
        if (alboOccupato)
            throw new InvalidOperationException("Numero di iscrizione all'albo già presente.");

        var passwordTemporanea = GenerateTempPassword();
        var created = await _authAdminService.CreateMedicoUserAsync(dto, passwordTemporanea, cancellationToken);

        try
        {
            // Attende il trigger handle_new_user e forza ruolo/anagrafica medico.
            Profilo? profilo = null;
            for (var attempt = 0; attempt < 8 && profilo is null; attempt++)
            {
                if (attempt > 0)
                    await Task.Delay(150, cancellationToken);

                profilo = await _profiloRepository.GetByIdAsync(created.UserId);
            }

            if (profilo is null)
                throw new InvalidOperationException("Profilo medico non creato dal trigger Auth.");

            profilo.Nome = dto.Nome.Trim();
            profilo.Cognome = dto.Cognome.Trim();
            profilo.CodiceFiscale = cf;
            profilo.DataNascita = dto.DataNascita;
            profilo.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
            profilo.Ruolo = RuoloUtente.Medico;

            var profiloOk = await _profiloRepository.UpdateAsync(profilo);
            if (!profiloOk)
                throw new InvalidOperationException("Impossibile aggiornare il profilo medico.");

            // UpdateAsync non tocca il ruolo: allinea esplicitamente.
            await _profiloRepository.UpdateRuoloAsync(created.UserId, RuoloUtente.Medico);

            var dettaglio = new MedicoDettaglio
            {
                MedicoId = created.UserId,
                Specializzazione = dto.Specializzazione.Trim(),
                NumeroIscrizioneAlbo = albo,
                Biografia = string.IsNullOrWhiteSpace(dto.Biografia) ? null : dto.Biografia.Trim(),
                TariffaVisita = dto.TariffaVisita,
                Attivo = true
            };

            var dettagliOk = await _medicoRepository.CreateMedicoDettagliAsync(dettaglio);
            if (!dettagliOk)
                throw new InvalidOperationException("Impossibile salvare i dettagli professionali del medico.");
        }
        catch
        {
            await _authAdminService.DeleteUserAsync(created.UserId, cancellationToken);
            throw;
        }

        return new MedicoCreateResponseDto
        {
            Id = created.UserId,
            Email = created.Email,
            PasswordTemporanea = passwordTemporanea,
            Messaggio =
                "Medico creato. Comunica la password temporanea: al primo accesso dovrà cambiarla."
        };
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

    private static string GenerateTempPassword(int length = 14)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        return string.Create(length, alphabet, static (span, chars) =>
        {
            Span<byte> bytes = stackalloc byte[span.Length];
            RandomNumberGenerator.Fill(bytes);
            for (var i = 0; i < span.Length; i++)
                span[i] = chars[bytes[i] % chars.Length];
        });
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
        return await CercaMediciAsync(specializzazione: specializzazione);
    }

    public async Task<IEnumerable<ProfiloDto>> CercaMediciAsync(
        string? specializzazione = null,
        string? nome = null)
    {
        var medici = await _medicoRepository.GetMediciAttiviAsync(specializzazione, nome);
        return medici.Select(m => MapProfilo(m.Profilo, MapDettagli(m.Dettagli)));
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
