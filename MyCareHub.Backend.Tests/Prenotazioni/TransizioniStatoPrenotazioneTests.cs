using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Models.DTOs.Prenotazione;
using MyCareHub.Backend.Tests.Support;

namespace MyCareHub.Backend.Tests.Prenotazioni;

public class TransizioniStatoPrenotazioneTests
{
    public static IEnumerable<object[]> TutteLeTransizioni()
    {
        foreach (var from in Enum.GetValues<StatoPrenotazione>())
        {
            foreach (var to in Enum.GetValues<StatoPrenotazione>())
                yield return [from, to];
        }
    }

    [Theory]
    [MemberData(nameof(TutteLeTransizioni))]
    public async Task CambiaStato_applica_solo_le_transizioni_consentite(
        StatoPrenotazione from,
        StatoPrenotazione to)
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(from, slotPrenotato: true);
        var consentita = TransizioneConsentita(from, to);

        var aggiornato = await fixture.Prenotazioni.CambiaStatoVisitaAsync(seed.PrenotazioneId, to);

        Assert.Equal(consentita, aggiornato);
        Assert.Equal(consentita && from != to ? to : from, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);
        Assert.Equal(consentita && to == StatoPrenotazione.Cancellata && from != to, !fixture.Db.Slots[seed.SlotId].IsPrenotato);
    }

    [Fact]
    public async Task CambiaStato_prenotazione_assente_non_modifica_nulla()
    {
        var fixture = new ClinicFixture();
        fixture.SeedVisita();

        var aggiornato = await fixture.Prenotazioni.CambiaStatoVisitaAsync(Guid.NewGuid(), StatoPrenotazione.Confermata);

        Assert.False(aggiornato);
        Assert.All(fixture.Db.Prenotazioni.Values, p => Assert.Equal(StatoPrenotazione.InAttesa, p.Stato));
    }

    [Fact]
    public async Task Cancellazione_non_libera_lo_slot_se_lo_stato_non_viene_persistito()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(StatoPrenotazione.Confermata);
        fixture.Db.RejectStatoUpdate = true;

        var aggiornato = await fixture.Prenotazioni.CambiaStatoVisitaAsync(
            seed.PrenotazioneId,
            StatoPrenotazione.Cancellata);

        Assert.False(aggiornato);
        Assert.Equal(StatoPrenotazione.Confermata, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);
        Assert.True(fixture.Db.Slots[seed.SlotId].IsPrenotato);
    }

    [Theory]
    [InlineData(StatoPrenotazione.InAttesa)]
    [InlineData(StatoPrenotazione.Confermata)]
    public async Task Il_medico_assegnato_puo_concludere_la_visita(StatoPrenotazione stato)
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(stato);

        var conclusa = await fixture.Prenotazioni.ConcludiVisitaAsync(
            seed.PrenotazioneId,
            seed.MedicoId,
            new PrenotazioneConclusioneDto { NoteMediche = "Visita conclusa regolarmente." });

        Assert.True(conclusa);
        Assert.Equal(StatoPrenotazione.Completata, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);
        Assert.Equal("Visita conclusa regolarmente.", fixture.Db.Prenotazioni[seed.PrenotazioneId].NoteMediche);
        Assert.True(fixture.Db.Slots[seed.SlotId].IsPrenotato);
    }

    [Theory]
    [InlineData(StatoPrenotazione.Completata)]
    [InlineData(StatoPrenotazione.Cancellata)]
    public async Task Una_visita_terminale_non_si_puo_concludere(StatoPrenotazione stato)
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(stato);

        var conclusa = await fixture.Prenotazioni.ConcludiVisitaAsync(
            seed.PrenotazioneId,
            seed.MedicoId,
            new PrenotazioneConclusioneDto { NoteMediche = "Visita conclusa regolarmente." });

        Assert.False(conclusa);
        Assert.Equal(stato, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);
        Assert.Null(fixture.Db.Prenotazioni[seed.PrenotazioneId].NoteMediche);
    }

    [Fact]
    public async Task Un_altro_medico_non_puo_concludere_la_visita()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(StatoPrenotazione.Confermata);

        var conclusa = await fixture.Prenotazioni.ConcludiVisitaAsync(
            seed.PrenotazioneId,
            seed.AltroMedicoId,
            new PrenotazioneConclusioneDto { NoteMediche = "Visita conclusa regolarmente." });

        Assert.False(conclusa);
        Assert.Equal(StatoPrenotazione.Confermata, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);
        Assert.Null(fixture.Db.Prenotazioni[seed.PrenotazioneId].NoteMediche);
    }

    [Fact]
    public async Task Conclusione_fallita_non_cambia_lo_stato()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(StatoPrenotazione.Confermata);
        fixture.Db.RejectNoteUpdate = true;

        var conclusa = await fixture.Prenotazioni.ConcludiVisitaAsync(
            seed.PrenotazioneId,
            seed.MedicoId,
            new PrenotazioneConclusioneDto { NoteMediche = "Visita conclusa regolarmente." });

        Assert.False(conclusa);
        Assert.Equal(StatoPrenotazione.Confermata, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);
    }

    [Fact]
    public async Task Se_lo_stato_finale_non_viene_salvato_la_visita_resta_aperta()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(StatoPrenotazione.Confermata);
        fixture.Db.RejectStatoUpdate = true;

        var conclusa = await fixture.Prenotazioni.ConcludiVisitaAsync(
            seed.PrenotazioneId,
            seed.MedicoId,
            new PrenotazioneConclusioneDto { NoteMediche = "Visita conclusa regolarmente." });

        Assert.False(conclusa);
        Assert.Equal(StatoPrenotazione.Confermata, fixture.Db.Prenotazioni[seed.PrenotazioneId].Stato);
        Assert.Equal("Visita conclusa regolarmente.", fixture.Db.Prenotazioni[seed.PrenotazioneId].NoteMediche);
    }

    private static bool TransizioneConsentita(StatoPrenotazione from, StatoPrenotazione to)
    {
        if (from == to)
            return true;

        if (to == StatoPrenotazione.Completata)
            return false;

        return (from, to) switch
        {
            (StatoPrenotazione.InAttesa, StatoPrenotazione.Confermata) => true,
            (StatoPrenotazione.InAttesa, StatoPrenotazione.Cancellata) => true,
            (StatoPrenotazione.Confermata, StatoPrenotazione.Cancellata) => true,
            _ => false
        };
    }
}
