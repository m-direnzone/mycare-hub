using Microsoft.AspNetCore.Mvc;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Models.DTOs.Prenotazione;
using MyCareHub.Backend.Tests.Support;

namespace MyCareHub.Backend.Tests.Prenotazioni;

public class PermessiPrenotazioneTests
{
    [Fact]
    public async Task Il_paziente_prenota_e_il_medico_non_puo_prenotare_al_suo_posto()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(conPrenotazione: false);
        var dto = new PrenotazioneCreateDto { MedicoId = seed.MedicoId, SlotId = seed.SlotId };

        var medico = await fixture.PrenotazioniController(TestPrincipals.Medico(seed.MedicoId)).PrenotaVisita(dto);
        Assert.IsType<ConflictObjectResult>(medico.Result);
        Assert.False(fixture.Db.Slots[seed.SlotId].IsPrenotato);

        var paziente = await fixture.PrenotazioniController(TestPrincipals.Paziente(seed.PazienteId)).PrenotaVisita(dto);
        var created = Assert.IsType<CreatedAtActionResult>(paziente.Result);
        var body = Assert.IsType<PrenotazioneDto>(created.Value);
        Assert.Equal(StatoPrenotazione.InAttesa, body.Stato);
        Assert.Equal(seed.PazienteId, body.PazienteId);
        Assert.True(fixture.Db.Slots[seed.SlotId].IsPrenotato);
    }

    [Fact]
    public async Task Il_medico_assegnato_conferma_e_un_medico_estraneo_no()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita();

        var estraneo = await fixture.PrenotazioniController(TestPrincipals.Medico(seed.AltroMedicoId))
            .CambiaStato(seed.PrenotazioneId, "confermata");
        Assert.IsType<ForbidResult>(estraneo);
        Assert.Equal(StatoPrenotazione.InAttesa, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);

        var assegnato = await fixture.PrenotazioniController(TestPrincipals.Medico(seed.MedicoId))
            .CambiaStato(seed.PrenotazioneId, "confermata");
        Assert.IsType<NoContentResult>(assegnato);
        Assert.Equal(StatoPrenotazione.Confermata, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);
    }

    [Fact]
    public async Task Il_paziente_puo_solo_cancellare_la_propria_visita()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita();
        var controller = fixture.PrenotazioniController(TestPrincipals.Paziente(seed.PazienteId));

        var conferma = await controller.CambiaStato(seed.PrenotazioneId, "confermata");
        Assert.IsType<ForbidResult>(conferma);
        Assert.Equal(StatoPrenotazione.InAttesa, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);

        var cancellazione = await controller.CambiaStato(seed.PrenotazioneId, "cancellata");
        Assert.IsType<NoContentResult>(cancellazione);
        Assert.Equal(StatoPrenotazione.Cancellata, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);
        Assert.False(fixture.Db.Slots[seed.SlotId].IsPrenotato);
    }

    [Fact]
    public async Task Paziente_estraneo_e_admin_non_cambiano_lo_stato()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita();

        var estraneo = await fixture.PrenotazioniController(TestPrincipals.Paziente(seed.EstraneoId))
            .CambiaStato(seed.PrenotazioneId, "cancellata");
        var admin = await fixture.PrenotazioniController(TestPrincipals.Admin(seed.AdminId))
            .CambiaStato(seed.PrenotazioneId, "confermata");

        Assert.IsType<ForbidResult>(estraneo);
        Assert.IsType<ForbidResult>(admin);
        Assert.Equal(StatoPrenotazione.InAttesa, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);
        Assert.True(fixture.Db.Slots[seed.SlotId].IsPrenotato);
    }

    [Fact]
    public async Task Il_medico_non_puo_impostare_in_attesa_o_completata_dal_cambio_stato()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(StatoPrenotazione.Confermata);
        var controller = fixture.PrenotazioniController(TestPrincipals.Medico(seed.MedicoId));

        var inAttesa = await controller.CambiaStato(seed.PrenotazioneId, "in_attesa");
        var completata = await controller.CambiaStato(seed.PrenotazioneId, "completata");

        Assert.IsType<BadRequestObjectResult>(inAttesa);
        Assert.IsType<BadRequestObjectResult>(completata);
        Assert.Equal(StatoPrenotazione.Confermata, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);
    }

    [Fact]
    public async Task Solo_il_medico_assegnato_conclude_la_visita()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(StatoPrenotazione.Confermata);
        var dto = new PrenotazioneConclusioneDto { NoteMediche = "Visita conclusa regolarmente." };

        var paziente = await fixture.PrenotazioniController(TestPrincipals.Paziente(seed.PazienteId))
            .ConcludiVisita(seed.PrenotazioneId, dto);
        var altroMedico = await fixture.PrenotazioniController(TestPrincipals.Medico(seed.AltroMedicoId))
            .ConcludiVisita(seed.PrenotazioneId, dto);
        var admin = await fixture.PrenotazioniController(TestPrincipals.Admin(seed.AdminId))
            .ConcludiVisita(seed.PrenotazioneId, dto);

        Assert.IsType<ForbidResult>(paziente);
        Assert.IsType<ForbidResult>(altroMedico);
        Assert.IsType<ForbidResult>(admin);
        Assert.Equal(StatoPrenotazione.Confermata, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);

        var assegnato = await fixture.PrenotazioniController(TestPrincipals.Medico(seed.MedicoId))
            .ConcludiVisita(seed.PrenotazioneId, dto);
        Assert.IsType<NoContentResult>(assegnato);
        Assert.Equal(StatoPrenotazione.Completata, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);
    }

    [Fact]
    public async Task Dopo_la_conclusione_la_cancellazione_e_rifiutata()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(StatoPrenotazione.Completata);

        var result = await fixture.PrenotazioniController(TestPrincipals.Medico(seed.MedicoId))
            .CambiaStato(seed.PrenotazioneId, "cancellata");

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(StatoPrenotazione.Completata, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);
        Assert.True(fixture.Db.Slots[seed.SlotId].IsPrenotato);
    }

    [Theory]
    [InlineData("paziente")]
    [InlineData("medico")]
    [InlineData("admin")]
    public async Task I_partecipanti_e_l_admin_leggono_la_prenotazione_un_estraneo_no(string ruolo)
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita();
        var principal = ruolo switch
        {
            "paziente" => TestPrincipals.Paziente(seed.PazienteId),
            "medico" => TestPrincipals.Medico(seed.MedicoId),
            _ => TestPrincipals.Admin(seed.AdminId)
        };

        var result = await fixture.PrenotazioniController(principal).GetPrenotazioneById(seed.PrenotazioneId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<PrenotazioneDto>(ok.Value);
        Assert.Equal(seed.PrenotazioneId, dto.Id);
    }

    [Fact]
    public async Task Un_estraneo_non_legge_la_prenotazione()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita();

        var result = await fixture.PrenotazioniController(TestPrincipals.Paziente(seed.EstraneoId))
            .GetPrenotazioneById(seed.PrenotazioneId);

        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task Senza_identita_le_operazioni_sono_non_autenticate()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita();
        var controller = fixture.PrenotazioniController(TestPrincipals.Anonimo());

        Assert.IsType<UnauthorizedResult>((await controller.GetPrenotazioneById(seed.PrenotazioneId)).Result);
        Assert.IsType<UnauthorizedResult>(await controller.CambiaStato(seed.PrenotazioneId, "cancellata"));
        Assert.Equal(StatoPrenotazione.InAttesa, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);
    }
}
