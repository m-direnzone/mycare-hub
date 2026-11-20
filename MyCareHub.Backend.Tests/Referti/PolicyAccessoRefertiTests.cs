using Microsoft.AspNetCore.Mvc;
using MyCareHub.Backend.Controllers;
using MyCareHub.Backend.Models.DTOs.Referto;
using MyCareHub.Backend.Services.Interfaces;
using MyCareHub.Backend.Tests.Support;

namespace MyCareHub.Backend.Tests.Referti;

public class PolicyAccessoRefertiTests
{
    [Fact]
    public async Task Il_referto_e_leggibile_solo_da_paziente_medico_emittente_e_admin()
    {
        var pazienteId = Guid.NewGuid();
        var medicoId = Guid.NewGuid();
        var altroMedicoId = Guid.NewGuid();
        var estraneoId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var referto = new RefertoDto
        {
            Id = Guid.NewGuid(),
            PazienteId = pazienteId,
            MedicoId = medicoId,
            Titolo = "Emocromo",
            DownloadUrl = "https://signed.example/referto",
            MedicoNomeCompleto = "Medico Test",
            MedicoSpecializzazione = "Cardiologia",
            DataEmissione = new DateOnly(2026, 1, 1)
        };
        var service = new RefertoServiceFinto { Referto = referto };

        Assert.IsType<OkObjectResult>((await Controller(service, TestPrincipals.Paziente(pazienteId)).GetRefertoById(referto.Id)).Result);
        Assert.IsType<OkObjectResult>((await Controller(service, TestPrincipals.Medico(medicoId)).GetRefertoById(referto.Id)).Result);
        Assert.IsType<OkObjectResult>((await Controller(service, TestPrincipals.Admin(adminId)).GetRefertoById(referto.Id)).Result);
        Assert.IsType<ForbidResult>((await Controller(service, TestPrincipals.Paziente(estraneoId)).GetRefertoById(referto.Id)).Result);
        Assert.IsType<ForbidResult>((await Controller(service, TestPrincipals.Medico(altroMedicoId)).GetRefertoById(referto.Id)).Result);
        Assert.IsType<UnauthorizedResult>((await Controller(service, TestPrincipals.Anonimo()).GetRefertoById(referto.Id)).Result);

        service.Referto = null;
        Assert.IsType<NotFoundObjectResult>((await Controller(service, TestPrincipals.Paziente(pazienteId)).GetRefertoById(referto.Id)).Result);
    }

    [Fact]
    public async Task Il_fascicolo_e_limitato_al_paziente_autenticato()
    {
        var pazienteId = Guid.NewGuid();
        var service = new RefertoServiceFinto();
        var controller = Controller(service, TestPrincipals.Paziente(pazienteId));

        var result = await controller.GetMioFascicoloSanitario();

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(pazienteId, service.FascicoloRichiesto);
        Assert.Empty(typeof(RefertiController).GetMethod(nameof(RefertiController.GetMioFascicoloSanitario))!.GetParameters());
    }

    [Fact]
    public async Task Salvare_un_referto_richiede_medico_paziente_e_prenotazione_coerenti()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita();
        var service = fixture.CreateReferti();
        var percorso = "paziente/medico/referto.pdf";

        Assert.Null(await service.SalvaMetaRefertoAsync(seed.PazienteId, Meta(seed.PazienteId, seed.PrenotazioneId), percorso));
        Assert.Null(await service.SalvaMetaRefertoAsync(seed.MedicoId, Meta(seed.MedicoId, null), percorso));
        Assert.Null(await service.SalvaMetaRefertoAsync(seed.AltroMedicoId, Meta(seed.PazienteId, seed.PrenotazioneId), percorso));
        Assert.Null(await service.SalvaMetaRefertoAsync(seed.MedicoId, Meta(seed.EstraneoId, seed.PrenotazioneId), percorso));
        Assert.Null(await service.SalvaMetaRefertoAsync(seed.MedicoId, Meta(seed.PazienteId, seed.PrenotazioneId), "  "));
        Assert.Empty(fixture.Db.Referti);

        RefertoDto? creato = null;
        Exception? erroreStorage = null;
        try
        {
            creato = await service.SalvaMetaRefertoAsync(
                seed.MedicoId,
                Meta(seed.PazienteId, seed.PrenotazioneId),
                percorso);
        }
        catch (Exception ex)
        {
            erroreStorage = ex;
        }

        var salvato = Assert.Single(fixture.Db.Referti.Values);
        Assert.Equal(seed.MedicoId, salvato.MedicoId);
        Assert.Equal(seed.PazienteId, salvato.PazienteId);
        Assert.Equal(seed.PrenotazioneId, salvato.PrenotazioneId);
        Assert.True(creato is not null || erroreStorage is not null);
    }

    private static RefertoCreateDto Meta(Guid pazienteId, Guid? prenotazioneId) => new()
    {
        PazienteId = pazienteId,
        PrenotazioneId = prenotazioneId,
        Titolo = "Referto",
        Descrizione = "Controllo"
    };

    private static RefertiController Controller(IRefertoService service, System.Security.Claims.ClaimsPrincipal user)
    {
        var controller = new RefertiController(service);
        TestPrincipals.Attach(controller, user);
        return controller;
    }

    private sealed class RefertoServiceFinto : IRefertoService
    {
        public RefertoDto? Referto { get; set; }
        public Guid? FascicoloRichiesto { get; set; }

        public Task<RefertoDto?> CaricaRefertoAsync(
            Guid medicoId,
            RefertoCreateDto dto,
            Stream fileStream,
            string fileName,
            string contentType) => Task.FromResult<RefertoDto?>(null);

        public Task<RefertoDto?> SalvaMetaRefertoAsync(Guid medicoId, RefertoCreateDto dto, string storageFilePath) =>
            Task.FromResult<RefertoDto?>(null);

        public Task<IEnumerable<RefertoDto>> GetFascicoloSanitarioPazienteAsync(Guid pazienteId)
        {
            FascicoloRichiesto = pazienteId;
            return Task.FromResult<IEnumerable<RefertoDto>>([]);
        }

        public Task<RefertoDto?> GetRefertoByIdAsync(Guid refertoId) =>
            Task.FromResult(Referto is not null && Referto.Id == refertoId ? Referto : null);
    }
}
