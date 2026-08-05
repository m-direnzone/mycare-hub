using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Extensions;
using MyCareHub.Backend.Models.DTOs.Chat;
using MyCareHub.Backend.Services.Interfaces;

namespace MyCareHub.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = $"{nameof(RuoloUtente.Medico)},{nameof(RuoloUtente.Paziente)},medico,paziente")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// Elenca le conversazioni dell'utente autenticato.
    /// </summary>
    [HttpGet("conversazioni")]
    [ProducesResponseType(typeof(IEnumerable<ConversazioneDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ConversazioneDto>>> GetMieConversazioni()
    {
        var utenteId = User.GetUserId();
        if (utenteId is null)
            return Unauthorized();

        var list = await _chatService.GetMieConversazioniAsync(utenteId.Value, IsMedico());
        return Ok(list);
    }

    /// <summary>
    /// Contatti disponibili (medico↔paziente legati da almeno una prenotazione non cancellata).
    /// </summary>
    [HttpGet("contatti")]
    [ProducesResponseType(typeof(IEnumerable<ChatContattoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ChatContattoDto>>> GetContatti()
    {
        var utenteId = User.GetUserId();
        if (utenteId is null)
            return Unauthorized();

        var list = await _chatService.GetContattiAsync(utenteId.Value, IsMedico());
        return Ok(list);
    }

    /// <summary>
    /// Recupera o crea una conversazione con il contatto indicato.
    /// </summary>
    [HttpPost("conversazioni")]
    [ProducesResponseType(typeof(ConversazioneDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ConversazioneDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ConversazioneDto>> GetOrCreateConversazione(
        [FromBody] ConversazioneCreateDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var utenteId = User.GetUserId();
        if (utenteId is null)
            return Unauthorized();

        if (dto.ContattoId == Guid.Empty)
            return BadRequest(new { message = "contattoId obbligatorio." });

        var conversazione = await _chatService.GetOrCreateConversazioneAsync(
            utenteId.Value,
            IsMedico(),
            dto.ContattoId);

        if (conversazione is null)
            return Conflict(new
            {
                message = "Impossibile aprire la chat: contatto non valido o nessuna prenotazione in comune."
            });

        return Ok(conversazione);
    }

    /// <summary>
    /// Dettaglio conversazione (solo partecipanti).
    /// </summary>
    [HttpGet("conversazioni/{id:guid}")]
    [ProducesResponseType(typeof(ConversazioneDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConversazioneDto>> GetConversazione([FromRoute] Guid id)
    {
        var utenteId = User.GetUserId();
        if (utenteId is null)
            return Unauthorized();

        var conversazione = await _chatService.GetConversazioneAsync(id, utenteId.Value);
        if (conversazione is null)
            return NotFound(new { message = "Conversazione non trovata." });

        return Ok(conversazione);
    }

    /// <summary>
    /// Storico messaggi di una conversazione.
    /// </summary>
    [HttpGet("conversazioni/{id:guid}/messaggi")]
    [ProducesResponseType(typeof(IEnumerable<MessaggioDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<MessaggioDto>>> GetMessaggi(
        [FromRoute] Guid id,
        [FromQuery] DateTimeOffset? after = null)
    {
        var utenteId = User.GetUserId();
        if (utenteId is null)
            return Unauthorized();

        var conversazione = await _chatService.GetConversazioneAsync(id, utenteId.Value);
        if (conversazione is null)
            return NotFound(new { message = "Conversazione non trovata." });

        var messaggi = await _chatService.GetMessaggiAsync(id, utenteId.Value, after);
        return Ok(messaggi);
    }

    /// <summary>
    /// Invia un messaggio nella conversazione.
    /// </summary>
    [HttpPost("conversazioni/{id:guid}/messaggi")]
    [ProducesResponseType(typeof(MessaggioDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessaggioDto>> InviaMessaggio(
        [FromRoute] Guid id,
        [FromBody] MessaggioCreateDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var utenteId = User.GetUserId();
        if (utenteId is null)
            return Unauthorized();

        var messaggio = await _chatService.InviaMessaggioAsync(id, utenteId.Value, dto);
        if (messaggio is null)
            return BadRequest(new { message = "Messaggio non inviabile (conversazione assente o contenuto non valido)." });

        return CreatedAtAction(nameof(GetMessaggi), new { id }, messaggio);
    }

    /// <summary>
    /// Segna come letti i messaggi ricevuti nella conversazione.
    /// </summary>
    [HttpPost("conversazioni/{id:guid}/letti")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SegnaLetti([FromRoute] Guid id)
    {
        var utenteId = User.GetUserId();
        if (utenteId is null)
            return Unauthorized();

        var ok = await _chatService.SegnaMessaggiLettiAsync(id, utenteId.Value);
        if (!ok)
            return NotFound(new { message = "Conversazione non trovata." });

        return NoContent();
    }

    private bool IsMedico() =>
        User.IsInRole(nameof(RuoloUtente.Medico)) || User.IsInRole("medico");
}
