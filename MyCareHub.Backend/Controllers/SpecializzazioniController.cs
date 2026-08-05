using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyCareHub.Backend.Models.DTOs.Admin;
using MyCareHub.Backend.Services.Interfaces;

namespace MyCareHub.Backend.Controllers;

/// <summary>
/// Catalogo specializzazioni leggibile da utenti autenticati (paziente/medico/admin).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SpecializzazioniController : ControllerBase
{
    private readonly IAdminService _adminService;

    public SpecializzazioniController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SpecializzazioneDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SpecializzazioneDto>>> GetAll()
    {
        return Ok(await _adminService.GetSpecializzazioniAsync());
    }
}
