using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Extensions;
using MyCareHub.Backend.Models.DTOs.Referto;
using MyCareHub.Backend.Services.Interfaces;

namespace MyCareHub.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RefertiController : ControllerBase
{
    private const long MaxUploadBytes = 10 * 1024 * 1024;

    private readonly IRefertoService _refertoService;

    public RefertiController(IRefertoService refertoService)
    {
        _refertoService = refertoService;
    }

    /// <summary>
    /// Carica un file PDF referto nello Storage ed inserisce i metadati nel database clinico.
    /// </summary>
    [HttpPost("carica")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    [Authorize(Roles = $"{nameof(RuoloUtente.Medico)},medico")]
    [ProducesResponseType(typeof(RefertoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<ActionResult<RefertoDto>> CaricaReferto(
        [FromForm] RefertoCreateDto dto,
        IFormFile file)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Il file PDF del referto è obbligatorio." });

        if (file.Length > MaxUploadBytes)
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new
            {
                message = "Il file supera il limite massimo di 10 MB."
            });

        var contentType = file.ContentType ?? string.Empty;
        var isPdf = contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            || file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

        if (!isPdf)
            return BadRequest(new { message = "Sono accettati solo file PDF." });

        var medicoId = User.GetUserId();
        if (medicoId is null)
            return Unauthorized();

        await using var stream = file.OpenReadStream();
        var referto = await _refertoService.CaricaRefertoAsync(
            medicoId.Value,
            dto,
            stream,
            file.FileName,
            contentType);

        if (referto is null)
            return BadRequest(new
            {
                message = "Upload fallito: paziente/prenotazione non validi o errore di storage."
            });

        return CreatedAtAction(
            nameof(GetRefertoById),
            new { id = referto.Id },
            referto);
    }

    /// <summary>
    /// Restituisce il Fascicolo Sanitario Elettronico del paziente autenticato.
    /// </summary>
    [HttpGet("fascicolo")]
    [Authorize(Roles = $"{nameof(RuoloUtente.Paziente)},paziente")]
    [ProducesResponseType(typeof(IEnumerable<RefertoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<RefertoDto>>> GetMioFascicoloSanitario()
    {
        var pazienteId = User.GetUserId();
        if (pazienteId is null)
            return Unauthorized();

        var fascicolo = await _refertoService.GetFascicoloSanitarioPazienteAsync(pazienteId.Value);
        return Ok(fascicolo);
    }

    /// <summary>
    /// Recupera un singolo referto (paziente proprietario o medico emittente).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RefertoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RefertoDto>> GetRefertoById([FromRoute] Guid id)
    {
        var utenteId = User.GetUserId();
        if (utenteId is null)
            return Unauthorized();

        var referto = await _refertoService.GetRefertoByIdAsync(id);
        if (referto is null)
            return NotFound(new { message = "Referto non trovato." });

        var isAdmin = User.IsInRole(nameof(RuoloUtente.Admin)) || User.IsInRole("admin");
        if (!isAdmin && referto.PazienteId != utenteId.Value && referto.MedicoId != utenteId.Value)
            return Forbid();

        return Ok(referto);
    }
}
