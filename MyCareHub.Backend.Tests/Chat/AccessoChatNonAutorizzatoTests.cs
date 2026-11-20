using Microsoft.AspNetCore.Mvc;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Models.DTOs.Chat;
using MyCareHub.Backend.Tests.Support;

namespace MyCareHub.Backend.Tests.Chat;

public class AccessoChatNonAutorizzatoTests
{
    [Fact]
    public async Task Un_estraneo_non_legge_non_scrive_e_non_segna_i_messaggi()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita();
        var conversazione = fixture.AddConversazione(seed.PazienteId, seed.MedicoId);
        var messaggio = fixture.AddMessaggio(conversazione.Id, seed.MedicoId, "Il referto è pronto");

        Assert.Null(await fixture.Chat.GetConversazioneAsync(conversazione.Id, seed.EstraneoId));
        Assert.Null(await fixture.Chat.GetConversazioneAsync(conversazione.Id, seed.AdminId));
        Assert.Empty(await fixture.Chat.GetMessaggiAsync(conversazione.Id, seed.EstraneoId));
        Assert.Null(await fixture.Chat.InviaMessaggioAsync(
            conversazione.Id,
            seed.EstraneoId,
            new MessaggioCreateDto { Contenuto = "posso entrare?" }));
        Assert.False(await fixture.Chat.SegnaMessaggiLettiAsync(conversazione.Id, seed.EstraneoId));

        Assert.Single(fixture.Db.Messaggi);
        Assert.Null(messaggio.LettoAt);
        Assert.Equal("Il referto è pronto", messaggio.Contenuto);

        var letti = await fixture.Chat.GetMessaggiAsync(conversazione.Id, seed.PazienteId);
        Assert.Contains(letti, m => m.Contenuto == "Il referto è pronto" && !m.IsMio);
        Assert.True(await fixture.Chat.SegnaMessaggiLettiAsync(conversazione.Id, seed.PazienteId));
        Assert.NotNull(messaggio.LettoAt);
    }

    [Theory]
    [InlineData(StatoPrenotazione.Cancellata, false)]
    [InlineData(StatoPrenotazione.InAttesa, true)]
    [InlineData(StatoPrenotazione.Confermata, true)]
    [InlineData(StatoPrenotazione.Completata, true)]
    public async Task La_chat_si_apre_solo_con_una_prenotazione_non_cancellata(
        StatoPrenotazione stato,
        bool consentita)
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(stato);

        var comePaziente = await fixture.Chat.GetOrCreateConversazioneAsync(seed.PazienteId, isMedico: false, seed.MedicoId);
        var comeMedico = await fixture.Chat.GetOrCreateConversazioneAsync(seed.MedicoId, isMedico: true, seed.PazienteId);

        if (consentita)
        {
            Assert.NotNull(comePaziente);
            Assert.NotNull(comeMedico);
            Assert.Single(fixture.Db.Conversazioni);
        }
        else
        {
            Assert.Null(comePaziente);
            Assert.Null(comeMedico);
            Assert.Empty(fixture.Db.Conversazioni);
        }
    }

    [Fact]
    public async Task Non_si_apre_una_chat_con_se_stessi_un_ruolo_errato_o_senza_rapporto_di_cura()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita();
        var altroPaziente = fixture.AddPaziente("AltroPaziente");

        Assert.Null(await fixture.Chat.GetOrCreateConversazioneAsync(seed.PazienteId, false, seed.PazienteId));
        Assert.Null(await fixture.Chat.GetOrCreateConversazioneAsync(seed.PazienteId, false, altroPaziente));
        Assert.Null(await fixture.Chat.GetOrCreateConversazioneAsync(seed.MedicoId, true, seed.AltroMedicoId));
        Assert.Null(await fixture.Chat.GetOrCreateConversazioneAsync(seed.AltroMedicoId, true, seed.PazienteId));
        Assert.Null(await fixture.Chat.GetOrCreateConversazioneAsync(seed.EstraneoId, false, seed.MedicoId));
        Assert.Empty(fixture.Db.Conversazioni);
    }

    [Fact]
    public async Task Il_controller_non_rivela_una_conversazione_a_chi_non_partecipa()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita();
        var conversazione = fixture.AddConversazione(seed.PazienteId, seed.MedicoId);
        fixture.AddMessaggio(conversazione.Id, seed.MedicoId, "Riservato");
        var messaggio = new MessaggioCreateDto { Contenuto = "intrusione" };

        Assert.IsType<NotFoundObjectResult>((await fixture.ChatController(TestPrincipals.Paziente(seed.EstraneoId))
            .GetConversazione(conversazione.Id)).Result);
        Assert.IsType<NotFoundObjectResult>((await fixture.ChatController(TestPrincipals.Admin(seed.AdminId))
            .GetConversazione(conversazione.Id)).Result);
        Assert.IsType<NotFoundObjectResult>((await fixture.ChatController(TestPrincipals.Medico(seed.AltroMedicoId))
            .GetMessaggi(conversazione.Id)).Result);
        Assert.IsType<BadRequestObjectResult>((await fixture.ChatController(TestPrincipals.Paziente(seed.EstraneoId))
            .InviaMessaggio(conversazione.Id, messaggio)).Result);
        Assert.IsType<NotFoundObjectResult>(await fixture.ChatController(TestPrincipals.Medico(seed.AltroMedicoId))
            .SegnaLetti(conversazione.Id));

        Assert.DoesNotContain(fixture.Db.Messaggi, m => m.Contenuto == "intrusione");
        Assert.IsType<OkObjectResult>((await fixture.ChatController(TestPrincipals.Paziente(seed.PazienteId))
            .GetConversazione(conversazione.Id)).Result);

        var senzaCura = fixture.SeedVisita(StatoPrenotazione.Cancellata);
        var apertura = await fixture.ChatController(TestPrincipals.Paziente(senzaCura.PazienteId))
            .GetOrCreateConversazione(new ConversazioneCreateDto { ContattoId = senzaCura.MedicoId });
        Assert.IsType<ConflictObjectResult>(apertura.Result);
    }

    [Fact]
    public async Task Le_conversazioni_elencate_sono_solo_quelle_dell_utente()
    {
        var fixture = new ClinicFixture();
        var prima = fixture.SeedVisita();
        var seconda = fixture.SeedVisita();
        fixture.AddConversazione(prima.PazienteId, prima.MedicoId);
        fixture.AddConversazione(seconda.PazienteId, seconda.MedicoId);

        var mie = (await fixture.Chat.GetMieConversazioniAsync(prima.PazienteId, isMedico: false)).ToList();
        var estranee = (await fixture.Chat.GetMieConversazioniAsync(prima.EstraneoId, isMedico: false)).ToList();

        Assert.Single(mie);
        Assert.Equal(prima.MedicoId, mie[0].MedicoId);
        Assert.Empty(estranee);
    }
}
