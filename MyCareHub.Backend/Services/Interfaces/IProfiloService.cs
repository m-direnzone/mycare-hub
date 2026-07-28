using MyCareHub.Backend.Models.DTOs.Auth;
using MyCareHub.Backend.Models.DTOs.Profilo;

namespace MyCareHub.Backend.Services.Interfaces;

public interface IProfiloService
{
    /// <summary>
    /// Recupera un profilo completo (se medico, include automaticamente albo e tariffa).
    /// </summary>
    Task<ProfiloDto?> GetProfiloCompletoAsync(Guid utenteId);

    /// <summary>
    /// Crea Auth user medico + profilo + dettagli professionali (flusso admin completo).
    /// </summary>
    Task<MedicoCreateResponseDto> CreaMedicoAsync(
        MedicoOnboardingDto dto,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completa l'onboarding del medico inserendo i dettagli specialistici nel DB.
    /// </summary>
    Task<bool> CompletaOnboardingMedicoAsync(Guid medicoId, MedicoOnboardingDto dto);

    /// <summary>
    /// Aggiorna i dati anagrafici del profilo (codice fiscale e ruolo immutabili).
    /// </summary>
    Task<bool> AggiornaProfiloAsync(Guid utenteId, ProfiloUpdateDto dto);

    /// <summary>
    /// Aggiorna i dettagli professionali di un medico già onboardato.
    /// </summary>
    Task<bool> AggiornaDettagliMedicoAsync(Guid medicoId, MedicoDettaglioUpdateDto dto);

    /// <summary>
    /// Elenco medici attivi per specializzazione (ricerca paziente).
    /// </summary>
    Task<IEnumerable<ProfiloDto>> GetMediciBySpecializzazioneAsync(string specializzazione);
}
