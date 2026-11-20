using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Models.DTOs.Prenotazione;
using MyCareHub.Backend.Models.DTOs.Slot;
using MyCareHub.Backend.Tests.Support;

namespace MyCareHub.Backend.Tests.Slot;

public class SlotSimultaneiTests
{
    [Fact]
    public async Task Piu_prenotazioni_contemporanee_occupano_lo_slot_una_sola_volta()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(conPrenotazione: false);
        var pazienti = Enumerable.Range(0, 24).Select(i => fixture.AddPaziente($"P{i}")).ToArray();

        var risultati = await Task.WhenAll(pazienti.Select(id => fixture.Prenotazioni.PrenotaVisitaAsync(
            id,
            new PrenotazioneCreateDto { MedicoId = seed.MedicoId, SlotId = seed.SlotId })));

        Assert.Equal(1, risultati.Count(r => r is not null));
        var prenotazione = Assert.Single(fixture.Db.Prenotazioni.Values);
        Assert.Equal(seed.SlotId, prenotazione.SlotId);
        Assert.Equal(StatoPrenotazione.InAttesa, prenotazione.Stato);
        Assert.Contains(prenotazione.PazienteId, pazienti);
        Assert.True(fixture.Db.Slots[seed.SlotId].IsPrenotato);
    }

    [Fact]
    public async Task La_seconda_prenotazione_sequenziale_dello_stesso_slot_fallisce()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(conPrenotazione: false);
        var altro = fixture.AddPaziente("Secondo");
        var dto = new PrenotazioneCreateDto { MedicoId = seed.MedicoId, SlotId = seed.SlotId };

        var prima = await fixture.Prenotazioni.PrenotaVisitaAsync(seed.PazienteId, dto);
        var seconda = await fixture.Prenotazioni.PrenotaVisitaAsync(altro, dto);

        Assert.NotNull(prima);
        Assert.Null(seconda);
        Assert.Single(fixture.Db.Prenotazioni);
    }

    [Fact]
    public async Task Lo_stesso_paziente_non_occupa_due_slot_sovrapposti()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(conPrenotazione: false);
        var sovrapposto = fixture.AddSlot(seed.AltroMedicoId, seed.Inizio);

        var prima = await fixture.Prenotazioni.PrenotaVisitaAsync(seed.PazienteId, new PrenotazioneCreateDto
        {
            MedicoId = seed.MedicoId,
            SlotId = seed.SlotId
        });
        var seconda = await fixture.Prenotazioni.PrenotaVisitaAsync(seed.PazienteId, new PrenotazioneCreateDto
        {
            MedicoId = seed.AltroMedicoId,
            SlotId = sovrapposto.Id
        });

        Assert.NotNull(prima);
        Assert.Null(seconda);
        Assert.False(sovrapposto.IsPrenotato);
        Assert.Single(fixture.Db.Prenotazioni);
    }

    [Fact]
    public async Task Una_prenotazione_cancellata_non_tiene_bloccato_l_orario_del_paziente()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(conPrenotazione: false);
        var sovrapposto = fixture.AddSlot(seed.AltroMedicoId, seed.Inizio);

        await fixture.Prenotazioni.PrenotaVisitaAsync(seed.PazienteId, new PrenotazioneCreateDto
        {
            MedicoId = seed.MedicoId,
            SlotId = seed.SlotId
        });
        var esistente = Assert.Single(fixture.Db.Prenotazioni.Values);
        Assert.True(await fixture.Prenotazioni.CambiaStatoVisitaAsync(esistente.Id, StatoPrenotazione.Cancellata));

        var nuova = await fixture.Prenotazioni.PrenotaVisitaAsync(seed.PazienteId, new PrenotazioneCreateDto
        {
            MedicoId = seed.AltroMedicoId,
            SlotId = sovrapposto.Id
        });

        Assert.NotNull(nuova);
        Assert.True(sovrapposto.IsPrenotato);
    }

    [Fact]
    public async Task Se_il_salvataggio_fallisce_lo_slot_viene_rilasciato()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(conPrenotazione: false);
        fixture.Db.RejectNextPrenotazioneCreate = true;

        var creata = await fixture.Prenotazioni.PrenotaVisitaAsync(seed.PazienteId, new PrenotazioneCreateDto
        {
            MedicoId = seed.MedicoId,
            SlotId = seed.SlotId
        });

        Assert.Null(creata);
        Assert.Empty(fixture.Db.Prenotazioni);
        Assert.False(fixture.Db.Slots[seed.SlotId].IsPrenotato);
    }

    [Fact]
    public async Task La_generazione_rifiuta_gli_slot_che_si_sovrappongono_tra_loro_o_con_quelli_esistenti()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(conPrenotazione: false);
        var inizio = seed.Inizio.AddDays(1);

        var interni = await fixture.Slots.GeneraAgendaBulkAsync(new SlotCreationBulkDto
        {
            MedicoId = seed.MedicoId,
            SlotRichiesti =
            [
                new TimeIntervalDto { DataOraInizio = inizio, DataOraFine = inizio.AddHours(1) },
                new TimeIntervalDto { DataOraInizio = inizio.AddMinutes(30), DataOraFine = inizio.AddHours(2) }
            ]
        });

        Assert.False(interni);
        Assert.Single(fixture.Db.Slots);

        var esistenti = await fixture.Slots.GeneraAgendaBulkAsync(new SlotCreationBulkDto
        {
            MedicoId = seed.MedicoId,
            SlotRichiesti =
            [
                new TimeIntervalDto { DataOraInizio = seed.Inizio.AddMinutes(-10), DataOraFine = seed.Fine }
            ]
        });

        Assert.False(esistenti);
        Assert.Single(fixture.Db.Slots);

        var validi = await fixture.Slots.GeneraAgendaBulkAsync(new SlotCreationBulkDto
        {
            MedicoId = seed.MedicoId,
            SlotRichiesti =
            [
                new TimeIntervalDto { DataOraInizio = inizio, DataOraFine = inizio.AddMinutes(30) },
                new TimeIntervalDto { DataOraInizio = inizio.AddMinutes(30), DataOraFine = inizio.AddHours(1) }
            ]
        });

        Assert.True(validi);
        Assert.Equal(3, fixture.Db.Slots.Count);
    }

    [Fact]
    public async Task Il_medico_crea_slot_solo_per_se_l_admin_per_il_medico_indicato()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(conPrenotazione: false);
        var inizio = seed.Inizio.AddDays(2);
        var richiesto = new SlotCreationBulkDto
        {
            MedicoId = seed.AltroMedicoId,
            SlotRichiesti = [new TimeIntervalDto { DataOraInizio = inizio, DataOraFine = inizio.AddMinutes(30) }]
        };

        var medico = await fixture.SlotController(TestPrincipals.Medico(seed.MedicoId)).CreazioneInBloccoSlot(richiesto);
        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(medico).StatusCode);
        Assert.Contains(fixture.Db.Slots.Values, s => s.MedicoId == seed.MedicoId && s.DataOraInizio == inizio);
        Assert.DoesNotContain(fixture.Db.Slots.Values, s => s.MedicoId == seed.AltroMedicoId && s.DataOraInizio == inizio);

        var adminVuoto = await fixture.SlotController(TestPrincipals.Admin(seed.AdminId)).CreazioneInBloccoSlot(
            new SlotCreationBulkDto { MedicoId = Guid.Empty, SlotRichiesti = richiesto.SlotRichiesti });
        Assert.IsType<BadRequestObjectResult>(adminVuoto);

        var admin = await fixture.SlotController(TestPrincipals.Admin(seed.AdminId)).CreazioneInBloccoSlot(
            new SlotCreationBulkDto
            {
                MedicoId = seed.AltroMedicoId,
                SlotRichiesti = [new TimeIntervalDto { DataOraInizio = inizio.AddHours(2), DataOraFine = inizio.AddHours(2).AddMinutes(30) }]
            });
        Assert.Equal(StatusCodes.Status201Created, Assert.IsType<ObjectResult>(admin).StatusCode);
        Assert.Contains(fixture.Db.Slots.Values, s => s.MedicoId == seed.AltroMedicoId);
    }

    [Fact]
    public async Task Il_paziente_non_genera_agenda()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(conPrenotazione: false);
        var prima = fixture.Db.Slots.Count;

        var result = await fixture.SlotController(TestPrincipals.Paziente(seed.PazienteId)).CreazioneInBloccoSlot(
            new SlotCreationBulkDto
            {
                MedicoId = seed.MedicoId,
                SlotRichiesti =
                [
                    new TimeIntervalDto { DataOraInizio = seed.Inizio.AddDays(3), DataOraFine = seed.Inizio.AddDays(3).AddMinutes(30) }
                ]
            });

        Assert.IsType<ConflictObjectResult>(result);
        Assert.Equal(prima, fixture.Db.Slots.Count);
    }

    [Fact]
    public async Task Eliminare_uno_slot_rispetta_proprietario_admin_e_prenotazione()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(slotPrenotato: true);
        var libero = fixture.AddSlot(seed.MedicoId, seed.Inizio.AddDays(1));
        var liberoAltro = fixture.AddSlot(seed.AltroMedicoId, seed.Inizio.AddDays(1));

        var medicoSuAltro = await fixture.SlotController(TestPrincipals.Medico(seed.MedicoId))
            .EliminaSlot(libero.Id, seed.AltroMedicoId);
        Assert.IsType<ForbidResult>(medicoSuAltro);
        Assert.Contains(libero.Id, fixture.Db.Slots.Keys);

        var prenotato = await fixture.SlotController(TestPrincipals.Medico(seed.MedicoId))
            .EliminaSlot(seed.SlotId, null);
        Assert.IsType<ConflictObjectResult>(prenotato);
        Assert.Contains(seed.SlotId, fixture.Db.Slots.Keys);

        var proprio = await fixture.SlotController(TestPrincipals.Medico(seed.MedicoId))
            .EliminaSlot(libero.Id, null);
        Assert.IsType<NoContentResult>(proprio);
        Assert.DoesNotContain(libero.Id, fixture.Db.Slots.Keys);

        var admin = await fixture.SlotController(TestPrincipals.Admin(seed.AdminId))
            .EliminaSlot(liberoAltro.Id, seed.AltroMedicoId);
        Assert.IsType<NoContentResult>(admin);
        Assert.DoesNotContain(liberoAltro.Id, fixture.Db.Slots.Keys);
    }

    [Fact]
    public async Task Il_paziente_non_vede_gli_slot_sovrapposti_alle_proprie_visite()
    {
        var fixture = new ClinicFixture();
        var seed = fixture.SeedVisita(StatoPrenotazione.Confermata, slotPrenotato: true);
        var libero = fixture.AddSlot(seed.MedicoId, seed.Inizio.AddMinutes(15));
        var altroPaziente = fixture.AddPaziente("Libero");

        var comePaziente = TestPrincipals.Sequence(await fixture.SlotController(TestPrincipals.Paziente(seed.PazienteId))
            .GetSlotDisponibili(seed.MedicoId));
        var comeAltro = TestPrincipals.Sequence(await fixture.SlotController(TestPrincipals.Paziente(altroPaziente))
            .GetSlotDisponibili(seed.MedicoId));
        var comeMedico = TestPrincipals.Sequence(await fixture.SlotController(TestPrincipals.Medico(seed.AltroMedicoId))
            .GetSlotDisponibili(seed.MedicoId));

        Assert.DoesNotContain(comePaziente, s => s.Id == libero.Id);
        Assert.Contains(comeAltro, s => s.Id == libero.Id);
        Assert.Contains(comeMedico, s => s.Id == libero.Id);
    }
}
