using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Extensions;
using MyCareHub.Backend.Models.DTOs.Slot;
using MyCareHub.Backend.Services.Interfaces;

namespace MyCareHub.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SlotController : ControllerBase
{
    private readonly ISlotService _slotService;

    public SlotController(ISlotService slotService)
    {
        _slotService = slotService;
    }

    /// <summary>
    /// Recupera tutti gli slot orari liberi e prenotabili di uno specifico medico.
    /// </summary>
    [HttpGet("medici/{medicoId:guid}/disponibili")]
    [ProducesResponseType(typeof(IEnumerable<SlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<SlotDto>>> GetSlotDisponibili(
        [FromRoute] Guid medicoId,
        [FromQuery] DateTimeOffset? dataInizio = null,
        [FromQuery] DateTimeOffset? dataFine = null)
    {
        if (dataFine.HasValue && dataInizio.HasValue && dataFine < dataInizio)
            return BadRequest(new { message = "dataFine deve essere successiva a dataInizio." });

        var slots = await _slotService.GetSlotDisponibiliMedicoAsync(medicoId, dataInizio, dataFine);
        return Ok(slots);
    }

    /// <summary>
    /// Agenda completa (liberi + prenotati) del medico autenticato.
    /// </summary>
    [HttpGet("mio")]
    [Authorize(Roles = $"{nameof(RuoloUtente.Medico)},medico")]
    [ProducesResponseType(typeof(IEnumerable<SlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<SlotDto>>> GetMiaAgenda(
        [FromQuery] DateTimeOffset? dataInizio = null,
        [FromQuery] DateTimeOffset? dataFine = null)
    {
        var medicoId = User.GetUserId();
        if (medicoId is null)
            return Unauthorized();

        if (dataFine.HasValue && dataInizio.HasValue && dataFine < dataInizio)
            return BadRequest(new { message = "dataFine deve essere successiva a dataInizio." });

        var slots = await _slotService.GetAgendaMedicoAsync(medicoId.Value, dataInizio, dataFine);
        return Ok(slots);
    }

    /// <summary>
    /// Genera massivamente nuovi slot orari nel calendario del medico (Medico/Admin).
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Roles = $"{nameof(RuoloUtente.Medico)},{nameof(RuoloUtente.Admin)},medico,admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreazioneInBloccoSlot([FromBody] SlotCreationBulkDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var utenteId = User.GetUserId();
        if (utenteId is null)
            return Unauthorized();

        var isAdmin = User.IsInRole(nameof(RuoloUtente.Admin)) || User.IsInRole("admin");
        if (!isAdmin)
            dto.MedicoId = utenteId.Value;
        else if (dto.MedicoId == Guid.Empty)
            return BadRequest(new { message = "MedicoId obbligatorio per operazioni Admin." });

        var creato = await _slotService.GeneraAgendaBulkAsync(dto);
        if (!creato)
            return Conflict(new
            {
                message = "Impossibile creare gli slot: medico non valido, intervalli non validi o sovrapposizione con slot esistenti."
            });

        return StatusCode(StatusCodes.Status201Created, new
        {
            message = "Agenda aggiornata.",
            medicoId = dto.MedicoId,
            slotCreati = dto.SlotRichiesti.Count
        });
    }

    /// <summary>
    /// Elimina uno slot libero dall'agenda del medico autenticato.
    /// </summary>
    [HttpDelete("{slotId:guid}")]
    [Authorize(Roles = $"{nameof(RuoloUtente.Medico)},{nameof(RuoloUtente.Admin)},medico,admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EliminaSlot(
        [FromRoute] Guid slotId,
        [FromQuery] Guid? medicoId = null)
    {
        var utenteId = User.GetUserId();
        if (utenteId is null)
            return Unauthorized();

        var isAdmin = User.IsInRole(nameof(RuoloUtente.Admin)) || User.IsInRole("admin");
        var targetMedicoId = isAdmin && medicoId.HasValue ? medicoId.Value : utenteId.Value;

        if (!isAdmin && medicoId.HasValue && medicoId.Value != utenteId.Value)
            return Forbid();

        var eliminato = await _slotService.EliminaSlotAsync(slotId, targetMedicoId);
        if (!eliminato)
            return Conflict(new
            {
                message = "Slot non eliminabile: inesistente, non di tua competenza o già prenotato."
            });

        return NoContent();
    }
}
