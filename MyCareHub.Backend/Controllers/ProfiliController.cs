using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Extensions;
using MyCareHub.Backend.Models.DTOs.Auth;
using MyCareHub.Backend.Models.DTOs.Profilo;
using MyCareHub.Backend.Services.Interfaces;

namespace MyCareHub.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfiliController : ControllerBase
{
    private readonly IProfiloService _profiloService;

    public ProfiliController(IProfiloService profiloService)
    {
        _profiloService = profiloService;
    }

    /// <summary>
    /// Recupera il profilo completo dell'utente attualmente autenticato.
    /// </summary>
    [HttpGet("mio")]
    [ProducesResponseType(typeof(ProfiloDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfiloDto>> GetMioProfilo()
    {
        var utenteId = User.GetUserId();
        if (utenteId is null)
            return Unauthorized();

        var profilo = await _profiloService.GetProfiloCompletoAsync(utenteId.Value);
        if (profilo is null)
            return NotFound(new { message = "Profilo non trovato." });

        return Ok(profilo);
    }

    /// <summary>
    /// Aggiorna i dati anagrafici del profilo autenticato.
    /// </summary>
    [HttpPut("mio")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AggiornaMioProfilo([FromBody] ProfiloUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var utenteId = User.GetUserId();
        if (utenteId is null)
            return Unauthorized();

        var aggiornato = await _profiloService.AggiornaProfiloAsync(utenteId.Value, dto);
        if (!aggiornato)
            return NotFound(new { message = "Profilo non trovato o non aggiornabile." });

        return NoContent();
    }

    /// <summary>
    /// Configura i dettagli clinici e professionali di un medico (onboarding amministrativo).
    /// </summary>
    [HttpPost("medici/{medicoId:guid}/onboarding")]
    [Authorize(Policy = nameof(RuoloUtente.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> OnboardingMedico(
        [FromRoute] Guid medicoId,
        [FromBody] MedicoOnboardingDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var esistente = await _profiloService.GetProfiloCompletoAsync(medicoId);
        if (esistente is null)
            return NotFound(new { message = "Medico non trovato." });

        if (esistente.DettagliMedico is not null)
            return Conflict(new { message = "Onboarding già completato per questo medico." });

        var ok = await _profiloService.CompletaOnboardingMedicoAsync(medicoId, dto);
        if (!ok)
            return BadRequest(new { message = "Impossibile completare l'onboarding del medico." });

        return NoContent();
    }

    /// <summary>
    /// Aggiorna specializzazione, biografia e tariffa del medico autenticato (o Admin).
    /// </summary>
    [HttpPut("medici/{medicoId:guid}/dettagli")]
    [Authorize(Roles = $"{nameof(RuoloUtente.Medico)},{nameof(RuoloUtente.Admin)},medico,admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AggiornaDettagliMedico(
        [FromRoute] Guid medicoId,
        [FromBody] MedicoDettaglioUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var utenteId = User.GetUserId();
        if (utenteId is null)
            return Unauthorized();

        var isAdmin = User.IsInRole(nameof(RuoloUtente.Admin)) || User.IsInRole("admin");
        if (!isAdmin && utenteId.Value != medicoId)
            return Forbid();

        var ok = await _profiloService.AggiornaDettagliMedicoAsync(medicoId, dto);
        if (!ok)
            return NotFound(new { message = "Dettagli medico non trovati. Completare prima l'onboarding." });

        return NoContent();
    }

    /// <summary>
    /// Ricerca medici attivi per specializzazione (lato paziente / autenticato).
    /// </summary>
    [HttpGet("medici")]
    [ProducesResponseType(typeof(IEnumerable<ProfiloDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<ProfiloDto>>> GetMediciBySpecializzazione(
        [FromQuery] string specializzazione)
    {
        if (string.IsNullOrWhiteSpace(specializzazione))
            return BadRequest(new { message = "Il parametro 'specializzazione' è obbligatorio." });

        var medici = await _profiloService.GetMediciBySpecializzazioneAsync(specializzazione);
        return Ok(medici);
    }
}
