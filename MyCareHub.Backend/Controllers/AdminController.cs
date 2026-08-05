using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Extensions;
using MyCareHub.Backend.Models.DTOs.Admin;
using MyCareHub.Backend.Services.Interfaces;

namespace MyCareHub.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = nameof(RuoloUtente.Admin))]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IProfiloService _profiloService;

    public AdminController(IAdminService adminService, IProfiloService profiloService)
    {
        _adminService = adminService;
        _profiloService = profiloService;
    }

    [HttpGet("kpi")]
    [ProducesResponseType(typeof(AdminKpiDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminKpiDto>> GetKpi(CancellationToken cancellationToken)
    {
        return Ok(await _adminService.GetKpiAsync(cancellationToken));
    }

    [HttpGet("medici")]
    [ProducesResponseType(typeof(IEnumerable<AdminMedicoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AdminMedicoDto>>> GetMedici(
        CancellationToken cancellationToken)
    {
        return Ok(await _adminService.GetMediciAsync(cancellationToken));
    }

    [HttpPut("medici/{medicoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AggiornaMedico(
        [FromRoute] Guid medicoId,
        [FromBody] AdminMedicoUpdateDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var (attoreId, attoreNome) = await ResolveAttoreAsync();
        if (attoreId is null)
            return Unauthorized();

        try
        {
            var ok = await _adminService.AggiornaMedicoAsync(
                medicoId, dto, attoreId.Value, attoreNome);
            if (!ok)
                return NotFound(new { message = "Medico non trovato." });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("medici/{medicoId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SoftDeleteMedico([FromRoute] Guid medicoId)
    {
        var (attoreId, attoreNome) = await ResolveAttoreAsync();
        if (attoreId is null)
            return Unauthorized();

        try
        {
            var ok = await _adminService.SoftDeleteMedicoAsync(medicoId, attoreId.Value, attoreNome);
            if (!ok)
                return NotFound(new { message = "Medico non trovato." });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("medici/{medicoId:guid}/riattiva")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RiattivaMedico([FromRoute] Guid medicoId)
    {
        var (attoreId, attoreNome) = await ResolveAttoreAsync();
        if (attoreId is null)
            return Unauthorized();

        var ok = await _adminService.RiattivaMedicoAsync(medicoId, attoreId.Value, attoreNome);
        if (!ok)
            return NotFound(new { message = "Medico non trovato." });

        return NoContent();
    }

    [HttpPost("audit/medico-creato")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> LogMedicoCreato([FromBody] AdminMedicoCreatoAuditDto dto)
    {
        var (attoreId, attoreNome) = await ResolveAttoreAsync();
        if (attoreId is null)
            return Unauthorized();

        var nomeMedico = $"{dto.Nome?.Trim()} {dto.Cognome?.Trim()}".Trim();
        await _adminService.LogAsync(
            attoreId,
            attoreNome,
            "Creazione medico",
            "medico",
            dto.MedicoId,
            $"L'amministratore {attoreNome} ha creato il medico {nomeMedico} ({dto.Email?.Trim().ToLowerInvariant()}).");

        return NoContent();
    }

    [HttpGet("pazienti")]
    [ProducesResponseType(typeof(IEnumerable<AdminPazienteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AdminPazienteDto>>> GetPazienti()
    {
        return Ok(await _adminService.GetPazientiAsync());
    }

    [HttpGet("prenotazioni")]
    [ProducesResponseType(typeof(IEnumerable<AdminPrenotazioneDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AdminPrenotazioneDto>>> GetPrenotazioni(
        [FromQuery] int? anno = null,
        [FromQuery] int? mese = null,
        [FromQuery] int? giorno = null)
    {
        return Ok(await _adminService.GetPrenotazioniAsync(anno, mese, giorno));
    }

    [HttpGet("audit")]
    [ProducesResponseType(typeof(IEnumerable<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AuditLogDto>>> GetAudit(
        [FromQuery] int limit = 200)
    {
        return Ok(await _adminService.GetAuditLogAsync(limit));
    }

    [HttpGet("specializzazioni")]
    [ProducesResponseType(typeof(IEnumerable<SpecializzazioneDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SpecializzazioneDto>>> GetSpecializzazioni()
    {
        return Ok(await _adminService.GetSpecializzazioniAsync());
    }

    [HttpPost("specializzazioni")]
    [ProducesResponseType(typeof(SpecializzazioneDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SpecializzazioneDto>> CreaSpecializzazione(
        [FromBody] SpecializzazioneCreateDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var (attoreId, attoreNome) = await ResolveAttoreAsync();
        if (attoreId is null)
            return Unauthorized();

        try
        {
            var created = await _adminService.CreaSpecializzazioneAsync(
                dto, attoreId.Value, attoreNome);
            return CreatedAtAction(nameof(GetSpecializzazioni), created);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("specializzazioni/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EliminaSpecializzazione([FromRoute] Guid id)
    {
        var (attoreId, attoreNome) = await ResolveAttoreAsync();
        if (attoreId is null)
            return Unauthorized();

        try
        {
            var ok = await _adminService.EliminaSpecializzazioneAsync(id, attoreId.Value, attoreNome);
            if (!ok)
                return NotFound(new { message = "Specializzazione non trovata." });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    private async Task<(Guid? Id, string Nome)> ResolveAttoreAsync()
    {
        var userId = User.GetUserId();
        if (userId is null)
            return (null, "Admin");

        var profilo = await _profiloService.GetProfiloCompletoAsync(userId.Value);
        var nome = profilo is null
            ? "Admin"
            : $"{profilo.Nome} {profilo.Cognome}".Trim();

        return (userId, string.IsNullOrWhiteSpace(nome) ? "Admin" : nome);
    }
}
