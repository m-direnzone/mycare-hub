using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Extensions;
using MyCareHub.Backend.Models.DTOs.Prenotazione;
using MyCareHub.Backend.Services.Interfaces;

namespace MyCareHub.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PrenotazioniController : ControllerBase
{
    private readonly IPrenotazioneService _prenotazioneService;

    public PrenotazioniController(IPrenotazioneService prenotazioneService)
    {
        _prenotazioneService = prenotazioneService;
    }

    /// <summary>
    /// Crea una nuova prenotazione bloccando atomicamente lo slot orario selezionato.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = $"{nameof(RuoloUtente.Paziente)},paziente")]
    [ProducesResponseType(typeof(PrenotazioneDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PrenotazioneDto>> PrenotaVisita([FromBody] PrenotazioneCreateDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var pazienteId = User.GetUserId();
        if (pazienteId is null)
            return Unauthorized();

        var prenotazione = await _prenotazioneService.PrenotaVisitaAsync(pazienteId.Value, dto);
        if (prenotazione is null)
            return Conflict(new
            {
                message = "Impossibile prenotare: slot non disponibile, non valido o già occupato."
            });

        return CreatedAtAction(
            nameof(GetPrenotazioneById),
            new { id = prenotazione.Id },
            prenotazione);
    }

    /// <summary>
    /// Elenca tutte le visite mediche del paziente autenticato.
    /// </summary>
    [HttpGet("paziente/mie-visite")]
    [Authorize(Roles = $"{nameof(RuoloUtente.Paziente)},paziente")]
    [ProducesResponseType(typeof(IEnumerable<PrenotazioneDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<PrenotazioneDto>>> GetMieiAppuntamentiPaziente()
    {
        var pazienteId = User.GetUserId();
        if (pazienteId is null)
            return Unauthorized();

        var visite = await _prenotazioneService.GetVisitePazienteAsync(pazienteId.Value);
        return Ok(visite);
    }

    /// <summary>
    /// Elenca tutti gli appuntamenti in agenda per il medico autenticato.
    /// </summary>
    [HttpGet("medico/agenda")]
    [Authorize(Roles = $"{nameof(RuoloUtente.Medico)},medico")]
    [ProducesResponseType(typeof(IEnumerable<PrenotazioneDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<PrenotazioneDto>>> GetAgendaMedico(
        [FromQuery] DateTimeOffset? dataInizio = null,
        [FromQuery] DateTimeOffset? dataFine = null)
    {
        var medicoId = User.GetUserId();
        if (medicoId is null)
            return Unauthorized();

        if (dataFine.HasValue && dataInizio.HasValue && dataFine < dataInizio)
            return BadRequest(new { message = "dataFine deve essere successiva a dataInizio." });

        var agenda = await _prenotazioneService.GetVisiteMedicoAsync(
            medicoId.Value,
            dataInizio,
            dataFine);

        return Ok(agenda);
    }

    /// <summary>
    /// Dettaglio di una prenotazione (solo partecipanti: paziente o medico).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PrenotazioneDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PrenotazioneDto>> GetPrenotazioneById([FromRoute] Guid id)
    {
        var utenteId = User.GetUserId();
        if (utenteId is null)
            return Unauthorized();

        var prenotazione = await _prenotazioneService.GetVisitaByIdAsync(id);
        if (prenotazione is null)
            return NotFound(new { message = "Prenotazione non trovata." });

        if (!CanAccessPrenotazione(prenotazione, utenteId.Value))
            return Forbid();

        return Ok(prenotazione);
    }

    /// <summary>
    /// Aggiorna lo stato di una prenotazione (accettazione/cancellazione).
    /// </summary>
    [HttpPatch("{id:guid}/stato")]
    [Authorize(Roles = $"{nameof(RuoloUtente.Medico)},{nameof(RuoloUtente.Paziente)},medico,paziente")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CambiaStato(
        [FromRoute] Guid id,
        [FromQuery] string nuovoStato)
    {
        if (string.IsNullOrWhiteSpace(nuovoStato))
            return BadRequest(new { message = "Il parametro 'nuovoStato' è obbligatorio." });

        if (!TryParseStato(nuovoStato, out var stato))
            return BadRequest(new
            {
                message = "Stato non valido. Valori ammessi: in_attesa, confermata, cancellata."
            });

        // Completata solo tramite ConcludiVisita
        if (stato == StatoPrenotazione.Completata)
            return BadRequest(new
            {
                message = "Per concludere una visita usare POST /{id}/concludi."
            });

        var utenteId = User.GetUserId();
        if (utenteId is null)
            return Unauthorized();

        var prenotazione = await _prenotazioneService.GetVisitaByIdAsync(id);
        if (prenotazione is null)
            return NotFound(new { message = "Prenotazione non trovata." });

        var isMedico = User.IsInRole(nameof(RuoloUtente.Medico)) || User.IsInRole("medico");
        var isPaziente = User.IsInRole(nameof(RuoloUtente.Paziente)) || User.IsInRole("paziente");

        if (isMedico && prenotazione.MedicoId == utenteId.Value)
        {
            // Medico: conferma o cancella
            if (stato is not (StatoPrenotazione.Confermata or StatoPrenotazione.Cancellata))
                return BadRequest(new { message = "Il medico può impostare solo 'confermata' o 'cancellata'." });
        }
        else if (isPaziente && prenotazione.PazienteId == utenteId.Value)
        {
            // Paziente: solo cancellazione
            if (stato != StatoPrenotazione.Cancellata)
                return Forbid();
        }
        else
        {
            return Forbid();
        }

        var aggiornato = await _prenotazioneService.CambiaStatoVisitaAsync(id, stato);
        if (!aggiornato)
            return Conflict(new { message = "Transizione di stato non consentita." });

        return NoContent();
    }

    /// <summary>
    /// Conclude ufficialmente la visita medica registrando il report clinico testuale.
    /// </summary>
    [HttpPost("{id:guid}/concludi")]
    [Authorize(Roles = $"{nameof(RuoloUtente.Medico)},medico")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConcludiVisita(
        [FromRoute] Guid id,
        [FromBody] PrenotazioneConclusioneDto dto)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var medicoId = User.GetUserId();
        if (medicoId is null)
            return Unauthorized();

        var prenotazione = await _prenotazioneService.GetVisitaByIdAsync(id);
        if (prenotazione is null)
            return NotFound(new { message = "Prenotazione non trovata." });

        if (prenotazione.MedicoId != medicoId.Value)
            return Forbid();

        var conclusa = await _prenotazioneService.ConcludiVisitaAsync(id, medicoId.Value, dto);
        if (!conclusa)
            return Conflict(new
            {
                message = "Visita non concludibile nello stato attuale."
            });

        return NoContent();
    }

    private bool CanAccessPrenotazione(PrenotazioneDto prenotazione, Guid utenteId)
    {
        if (User.IsInRole(nameof(RuoloUtente.Admin)) || User.IsInRole("admin"))
            return true;

        return prenotazione.MedicoId == utenteId || prenotazione.PazienteId == utenteId;
    }

    private static bool TryParseStato(string value, out StatoPrenotazione stato)
    {
        var normalized = value.Trim().ToLowerInvariant().Replace('-', '_');

        switch (normalized)
        {
            case "in_attesa":
            case "inattesa":
                stato = StatoPrenotazione.InAttesa;
                return true;
            case "confermata":
                stato = StatoPrenotazione.Confermata;
                return true;
            case "completata":
                stato = StatoPrenotazione.Completata;
                return true;
            case "cancellata":
                stato = StatoPrenotazione.Cancellata;
                return true;
            default:
                return Enum.TryParse(value, ignoreCase: true, out stato);
        }
    }
}
