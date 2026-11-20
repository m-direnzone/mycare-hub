using Microsoft.Extensions.Configuration;
using MyCareHub.Backend.Controllers;
using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Repositories.Interfaces;
using MyCareHub.Backend.Services.Implementations;
using Supabase;

namespace MyCareHub.Backend.Tests.Support;

internal sealed class ClinicDb
{
    public object Gate { get; } = new();
    public Dictionary<Guid, Profilo> Profili { get; } = new();
    public Dictionary<Guid, MedicoDettaglio> Medici { get; } = new();
    public Dictionary<Guid, SlotDisponibilita> Slots { get; } = new();
    public Dictionary<Guid, Prenotazione> Prenotazioni { get; } = new();
    public Dictionary<Guid, RefertoMedico> Referti { get; } = new();
    public Dictionary<Guid, Conversazione> Conversazioni { get; } = new();
    public List<Messaggio> Messaggi { get; } = new();
    public bool RejectNextPrenotazioneCreate;
    public bool RejectStatoUpdate;
    public bool RejectNoteUpdate;
}

internal sealed class VisitaSeed
{
    public required Guid PazienteId { get; init; }
    public required Guid MedicoId { get; init; }
    public required Guid AltroMedicoId { get; init; }
    public required Guid EstraneoId { get; init; }
    public required Guid AdminId { get; init; }
    public required Guid SlotId { get; init; }
    public required Guid PrenotazioneId { get; init; }
    public required DateTimeOffset Inizio { get; init; }
    public required DateTimeOffset Fine { get; init; }
}

internal sealed class ClinicFixture
{
    public ClinicDb Db { get; } = new();
    public PrenotazioneService Prenotazioni { get; }
    public SlotService Slots { get; }
    public ChatService Chat { get; }

    private readonly InMemoryProfiloRepository _profili;
    private readonly InMemoryMedicoRepository _medici;
    private readonly InMemoryPrenotazioneRepository _prenotazioni;
    private readonly InMemoryRefertoRepository _referti;

    public ClinicFixture()
    {
        _profili = new InMemoryProfiloRepository(Db);
        _medici = new InMemoryMedicoRepository(Db);
        var slot = new InMemorySlotRepository(Db);
        _prenotazioni = new InMemoryPrenotazioneRepository(Db);
        _referti = new InMemoryRefertoRepository(Db);
        var conversazioni = new InMemoryConversazioneRepository(Db);
        var messaggi = new InMemoryMessaggioRepository(Db);

        Prenotazioni = new PrenotazioneService(_prenotazioni, slot, _profili, _medici);
        Slots = new SlotService(slot, _medici, _profili, _prenotazioni);
        Chat = new ChatService(conversazioni, messaggi, _profili, _prenotazioni, _medici);
    }

    public RefertoService CreateReferti()
    {
        var client = new Client(
            "http://127.0.0.1:9",
            "anon-test-key",
            new SupabaseOptions
            {
                AutoRefreshToken = false,
                AutoConnectRealtime = false
            });

        return new RefertoService(
            _referti,
            _profili,
            _medici,
            _prenotazioni,
            client,
            new ConfigurationBuilder().Build());
    }

    public PrenotazioniController PrenotazioniController(System.Security.Claims.ClaimsPrincipal user)
    {
        var controller = new PrenotazioniController(Prenotazioni);
        TestPrincipals.Attach(controller, user);
        return controller;
    }

    public SlotController SlotController(System.Security.Claims.ClaimsPrincipal user)
    {
        var controller = new SlotController(Slots);
        TestPrincipals.Attach(controller, user);
        return controller;
    }

    public ChatController ChatController(System.Security.Claims.ClaimsPrincipal user)
    {
        var controller = new ChatController(Chat);
        TestPrincipals.Attach(controller, user);
        return controller;
    }

    public VisitaSeed SeedVisita(
        StatoPrenotazione stato = StatoPrenotazione.InAttesa,
        bool slotPrenotato = true,
        bool conPrenotazione = true)
    {
        var pazienteId = Guid.NewGuid();
        var medicoId = Guid.NewGuid();
        var altroMedicoId = Guid.NewGuid();
        var estraneoId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var slotId = Guid.NewGuid();
        var prenotazioneId = conPrenotazione ? Guid.NewGuid() : Guid.Empty;
        var inizio = DateTimeOffset.UtcNow.AddDays(7);
        var fine = inizio.AddMinutes(30);
        var now = DateTimeOffset.UtcNow;

        AddProfilo(pazienteId, RuoloUtente.Paziente, "Paziente");
        AddProfilo(medicoId, RuoloUtente.Medico, "Medico");
        AddProfilo(altroMedicoId, RuoloUtente.Medico, "Altro");
        AddProfilo(estraneoId, RuoloUtente.Paziente, "Estraneo");
        AddProfilo(adminId, RuoloUtente.Admin, "Admin");
        AddMedico(medicoId);
        AddMedico(altroMedicoId);

        Db.Slots[slotId] = new SlotDisponibilita
        {
            Id = slotId,
            MedicoId = medicoId,
            DataOraInizio = inizio,
            DataOraFine = fine,
            IsPrenotato = conPrenotazione && slotPrenotato,
            CreatoAt = now
        };

        if (conPrenotazione)
        {
            Db.Prenotazioni[prenotazioneId] = new Prenotazione
            {
                Id = prenotazioneId,
                PazienteId = pazienteId,
                MedicoId = medicoId,
                SlotId = slotId,
                Stato = stato,
                CreatoAt = now,
                AggiornatoAt = now
            };
        }

        return new VisitaSeed
        {
            PazienteId = pazienteId,
            MedicoId = medicoId,
            AltroMedicoId = altroMedicoId,
            EstraneoId = estraneoId,
            AdminId = adminId,
            SlotId = slotId,
            PrenotazioneId = prenotazioneId,
            Inizio = inizio,
            Fine = fine
        };
    }

    public Guid AddPaziente(string nome)
    {
        var id = Guid.NewGuid();
        AddProfilo(id, RuoloUtente.Paziente, nome);
        return id;
    }

    public SlotDisponibilita AddSlot(Guid medicoId, DateTimeOffset inizio, bool prenotato = false)
    {
        var slot = new SlotDisponibilita
        {
            Id = Guid.NewGuid(),
            MedicoId = medicoId,
            DataOraInizio = inizio,
            DataOraFine = inizio.AddMinutes(30),
            IsPrenotato = prenotato,
            CreatoAt = DateTimeOffset.UtcNow
        };
        Db.Slots[slot.Id] = slot;
        return slot;
    }

    public Conversazione AddConversazione(Guid pazienteId, Guid medicoId)
    {
        var now = DateTimeOffset.UtcNow;
        var conversazione = new Conversazione
        {
            Id = Guid.NewGuid(),
            PazienteId = pazienteId,
            MedicoId = medicoId,
            CreatoAt = now,
            AggiornatoAt = now
        };
        Db.Conversazioni[conversazione.Id] = conversazione;
        return conversazione;
    }

    public Messaggio AddMessaggio(Guid conversazioneId, Guid mittenteId, string contenuto)
    {
        var messaggio = new Messaggio
        {
            Id = Guid.NewGuid(),
            ConversazioneId = conversazioneId,
            MittenteId = mittenteId,
            Contenuto = contenuto,
            CreatoAt = DateTimeOffset.UtcNow
        };
        Db.Messaggi.Add(messaggio);
        return messaggio;
    }

    private void AddProfilo(Guid id, RuoloUtente ruolo, string nome)
    {
        var now = DateTimeOffset.UtcNow;
        Db.Profili[id] = new Profilo
        {
            Id = id,
            Nome = nome,
            Cognome = "Test",
            CodiceFiscale = id.ToString("N")[..16].ToUpperInvariant(),
            DataNascita = new DateOnly(1990, 1, 1),
            Ruolo = ruolo,
            CreatoAt = now,
            AggiornatoAt = now
        };
    }

    private void AddMedico(Guid id)
    {
        Db.Medici[id] = new MedicoDettaglio
        {
            MedicoId = id,
            Specializzazione = "Cardiologia",
            NumeroIscrizioneAlbo = id.ToString("N")[..8],
            TariffaVisita = 80,
            Attivo = true,
            PasswordCambiata = true
        };
    }
}

internal sealed class InMemoryProfiloRepository(ClinicDb db) : IProfiloRepository
{
    public Task<Profilo?> GetByIdAsync(Guid id)
    {
        lock (db.Gate)
            return Task.FromResult(db.Profili.TryGetValue(id, out var profilo) ? profilo : null);
    }

    public Task<Profilo?> GetByCodiceFiscaleAsync(string codiceFiscale)
    {
        lock (db.Gate)
            return Task.FromResult(db.Profili.Values.FirstOrDefault(p => p.CodiceFiscale == codiceFiscale));
    }

    public Task<bool> UpdateAsync(Profilo profilo) => throw new NotSupportedException();

    public Task<bool> UpdateRuoloAsync(Guid id, RuoloUtente ruolo) => throw new NotSupportedException();

    public Task<IEnumerable<Profilo>> GetByRuoloAsync(RuoloUtente ruolo)
    {
        lock (db.Gate)
            return Task.FromResult<IEnumerable<Profilo>>(db.Profili.Values.Where(p => p.Ruolo == ruolo).ToList());
    }

    public Task<IEnumerable<Profilo>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        lock (db.Gate)
        {
            var set = ids.ToHashSet();
            return Task.FromResult<IEnumerable<Profilo>>(db.Profili.Values.Where(p => set.Contains(p.Id)).ToList());
        }
    }
}

internal sealed class InMemoryMedicoRepository(ClinicDb db) : IMedicoRepository
{
    public Task<bool> CreateMedicoDettagliAsync(MedicoDettaglio dettaglio) => throw new NotSupportedException();

    public Task<MedicoDettaglio?> GetDettagliByIdAsync(Guid medicoId)
    {
        lock (db.Gate)
            return Task.FromResult(db.Medici.TryGetValue(medicoId, out var dettaglio) ? dettaglio : null);
    }

    public Task<IEnumerable<MedicoDettaglio>> GetDettagliByIdsAsync(IEnumerable<Guid> medicoIds)
    {
        lock (db.Gate)
        {
            var set = medicoIds.ToHashSet();
            return Task.FromResult<IEnumerable<MedicoDettaglio>>(db.Medici.Values.Where(d => set.Contains(d.MedicoId)).ToList());
        }
    }

    public Task<bool> UpdateMedicoDettagliAsync(MedicoDettaglio dettaglio) => throw new NotSupportedException();

    public Task<bool> ExistsByNumeroAlboAsync(string numeroIscrizioneAlbo) => Task.FromResult(false);

    public Task<bool> MarkPasswordCambiataAsync(Guid medicoId) => throw new NotSupportedException();

    public Task<IEnumerable<Profilo>> GetMediciBySpecializzazioneAsync(string specializzazione) =>
        Task.FromResult<IEnumerable<Profilo>>([]);

    public Task<IEnumerable<(Profilo Profilo, MedicoDettaglio Dettagli)>> GetMediciAttiviAsync(string? specializzazione = null, string? nome = null) =>
        Task.FromResult<IEnumerable<(Profilo, MedicoDettaglio)>>([]);

    public Task<IEnumerable<(Profilo Profilo, MedicoDettaglio Dettagli)>> GetTuttiMediciAsync() =>
        Task.FromResult<IEnumerable<(Profilo, MedicoDettaglio)>>([]);

    public Task<IEnumerable<MedicoDettaglio>> GetAllAttiviAsync() =>
        Task.FromResult<IEnumerable<MedicoDettaglio>>([]);

    public Task<IEnumerable<MedicoDettaglio>> GetAllDettagliAsync() =>
        Task.FromResult<IEnumerable<MedicoDettaglio>>([]);
}

internal sealed class InMemorySlotRepository(ClinicDb db) : ISlotDisponibilitaRepository
{
    public Task<SlotDisponibilita?> GetByIdAsync(Guid id)
    {
        lock (db.Gate)
            return Task.FromResult(db.Slots.TryGetValue(id, out var slot) ? slot : null);
    }

    public Task<IEnumerable<SlotDisponibilita>> GetByMedicoIdAsync(
        Guid medicoId,
        DateTimeOffset? dataInizio = null,
        DateTimeOffset? dataFine = null,
        bool? isPrenotato = null)
    {
        lock (db.Gate)
        {
            IEnumerable<SlotDisponibilita> query = db.Slots.Values.Where(s => s.MedicoId == medicoId);
            if (dataInizio.HasValue)
                query = query.Where(s => s.DataOraInizio >= dataInizio.Value);
            if (dataFine.HasValue)
                query = query.Where(s => s.DataOraInizio <= dataFine.Value);
            if (isPrenotato.HasValue)
                query = query.Where(s => s.IsPrenotato == isPrenotato.Value);

            return Task.FromResult<IEnumerable<SlotDisponibilita>>(query.OrderBy(s => s.DataOraInizio).ToList());
        }
    }

    public Task<SlotDisponibilita?> CreateAsync(SlotDisponibilita slot) => throw new NotSupportedException();

    public Task<bool> CreateBulkAsync(IEnumerable<SlotDisponibilita> slotList)
    {
        lock (db.Gate)
        {
            var slots = slotList.ToList();
            if (slots.Count == 0)
                return Task.FromResult(false);

            foreach (var slot in slots)
                db.Slots[slot.Id] = slot;

            return Task.FromResult(true);
        }
    }

    public Task<bool> UpdateStatoPrenotazioneAsync(Guid id, bool isPrenotato)
    {
        lock (db.Gate)
        {
            if (!db.Slots.TryGetValue(id, out var slot))
                return Task.FromResult(false);

            slot.IsPrenotato = isPrenotato;
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryLockSlotAsync(Guid id)
    {
        lock (db.Gate)
        {
            if (!db.Slots.TryGetValue(id, out var slot) || slot.IsPrenotato)
                return Task.FromResult(false);

            slot.IsPrenotato = true;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        lock (db.Gate)
        {
            if (!db.Slots.TryGetValue(id, out var slot) || slot.IsPrenotato)
                return Task.FromResult(false);

            db.Slots.Remove(id);
            return Task.FromResult(true);
        }
    }

    public Task<IEnumerable<SlotDisponibilita>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        lock (db.Gate)
        {
            var set = ids.ToHashSet();
            return Task.FromResult<IEnumerable<SlotDisponibilita>>(db.Slots.Values.Where(s => set.Contains(s.Id)).ToList());
        }
    }
}

internal sealed class InMemoryPrenotazioneRepository(ClinicDb db) : IPrenotazioneRepository
{
    public Task<Prenotazione?> GetByIdAsync(Guid id)
    {
        lock (db.Gate)
            return Task.FromResult(db.Prenotazioni.TryGetValue(id, out var prenotazione) ? prenotazione : null);
    }

    public Task<IEnumerable<Prenotazione>> GetByPazienteIdAsync(Guid pazienteId)
    {
        lock (db.Gate)
            return Task.FromResult<IEnumerable<Prenotazione>>(
                db.Prenotazioni.Values.Where(p => p.PazienteId == pazienteId).ToList());
    }

    public Task<IEnumerable<Prenotazione>> GetByMedicoIdAsync(
        Guid medicoId,
        DateTimeOffset? dataInizio = null,
        DateTimeOffset? dataFine = null)
    {
        lock (db.Gate)
        {
            IEnumerable<Prenotazione> query = db.Prenotazioni.Values.Where(p => p.MedicoId == medicoId);
            if (dataInizio is not null || dataFine is not null)
            {
                var slotIds = db.Slots.Values
                    .Where(s => s.MedicoId == medicoId)
                    .Where(s => !dataInizio.HasValue || s.DataOraInizio >= dataInizio.Value)
                    .Where(s => !dataFine.HasValue || s.DataOraInizio <= dataFine.Value)
                    .Select(s => s.Id)
                    .ToHashSet();
                query = query.Where(p => slotIds.Contains(p.SlotId));
            }

            return Task.FromResult<IEnumerable<Prenotazione>>(query.ToList());
        }
    }

    public Task<Prenotazione?> CreateAsync(Prenotazione prenotazione)
    {
        lock (db.Gate)
        {
            if (db.RejectNextPrenotazioneCreate)
            {
                db.RejectNextPrenotazioneCreate = false;
                return Task.FromResult<Prenotazione?>(null);
            }

            db.Prenotazioni[prenotazione.Id] = prenotazione;
            return Task.FromResult<Prenotazione?>(prenotazione);
        }
    }

    public Task<bool> UpdateStatoAsync(Guid id, StatoPrenotazione nuovoStato)
    {
        lock (db.Gate)
        {
            if (db.RejectStatoUpdate || !db.Prenotazioni.TryGetValue(id, out var prenotazione))
                return Task.FromResult(false);

            prenotazione.Stato = nuovoStato;
            prenotazione.AggiornatoAt = DateTimeOffset.UtcNow;
            return Task.FromResult(true);
        }
    }

    public Task<bool> UpdateNoteMedicheAsync(Guid id, string noteMediche)
    {
        lock (db.Gate)
        {
            if (db.RejectNoteUpdate || !db.Prenotazioni.TryGetValue(id, out var prenotazione))
                return Task.FromResult(false);

            prenotazione.NoteMediche = noteMediche;
            prenotazione.AggiornatoAt = DateTimeOffset.UtcNow;
            return Task.FromResult(true);
        }
    }

    public Task<IEnumerable<Prenotazione>> GetAllAsync(DateTimeOffset? dataInizio = null, DateTimeOffset? dataFine = null)
    {
        lock (db.Gate)
            return Task.FromResult<IEnumerable<Prenotazione>>(db.Prenotazioni.Values.ToList());
    }
}

internal sealed class InMemoryRefertoRepository(ClinicDb db) : IRefertoMedicoRepository
{
    public Task<RefertoMedico?> GetByIdAsync(Guid id)
    {
        lock (db.Gate)
            return Task.FromResult(db.Referti.TryGetValue(id, out var referto) ? referto : null);
    }

    public Task<IEnumerable<RefertoMedico>> GetByPazienteIdAsync(Guid pazienteId)
    {
        lock (db.Gate)
            return Task.FromResult<IEnumerable<RefertoMedico>>(db.Referti.Values.Where(r => r.PazienteId == pazienteId).ToList());
    }

    public Task<IEnumerable<RefertoMedico>> GetByMedicoIdAsync(Guid medicoId)
    {
        lock (db.Gate)
            return Task.FromResult<IEnumerable<RefertoMedico>>(db.Referti.Values.Where(r => r.MedicoId == medicoId).ToList());
    }

    public Task<RefertoMedico?> CreateAsync(RefertoMedico referto)
    {
        lock (db.Gate)
        {
            db.Referti[referto.Id] = referto;
            return Task.FromResult<RefertoMedico?>(referto);
        }
    }

    public Task<bool> DeleteAsync(Guid id)
    {
        lock (db.Gate)
            return Task.FromResult(db.Referti.Remove(id));
    }
}

internal sealed class InMemoryConversazioneRepository(ClinicDb db) : IConversazioneRepository
{
    public Task<Conversazione?> GetByIdAsync(Guid id)
    {
        lock (db.Gate)
            return Task.FromResult(db.Conversazioni.TryGetValue(id, out var conversazione) ? conversazione : null);
    }

    public Task<Conversazione?> GetByPartecipantiAsync(Guid pazienteId, Guid medicoId)
    {
        lock (db.Gate)
            return Task.FromResult(db.Conversazioni.Values.FirstOrDefault(c =>
                c.PazienteId == pazienteId && c.MedicoId == medicoId));
    }

    public Task<IEnumerable<Conversazione>> GetByPazienteIdAsync(Guid pazienteId)
    {
        lock (db.Gate)
            return Task.FromResult<IEnumerable<Conversazione>>(
                db.Conversazioni.Values.Where(c => c.PazienteId == pazienteId).ToList());
    }

    public Task<IEnumerable<Conversazione>> GetByMedicoIdAsync(Guid medicoId)
    {
        lock (db.Gate)
            return Task.FromResult<IEnumerable<Conversazione>>(
                db.Conversazioni.Values.Where(c => c.MedicoId == medicoId).ToList());
    }

    public Task<Conversazione?> CreateAsync(Conversazione conversazione)
    {
        lock (db.Gate)
        {
            db.Conversazioni[conversazione.Id] = conversazione;
            return Task.FromResult<Conversazione?>(conversazione);
        }
    }

    public Task<bool> TouchUltimoMessaggioAsync(Guid id, DateTimeOffset at)
    {
        lock (db.Gate)
        {
            if (!db.Conversazioni.TryGetValue(id, out var conversazione))
                return Task.FromResult(false);

            conversazione.UltimoMessaggioAt = at;
            conversazione.AggiornatoAt = at;
            return Task.FromResult(true);
        }
    }
}

internal sealed class InMemoryMessaggioRepository(ClinicDb db) : IMessaggioRepository
{
    public Task<IEnumerable<Messaggio>> GetByConversazioneIdAsync(
        Guid conversazioneId,
        DateTimeOffset? after = null,
        int limit = 100)
    {
        lock (db.Gate)
        {
            IEnumerable<Messaggio> query = db.Messaggi
                .Where(m => m.ConversazioneId == conversazioneId)
                .OrderBy(m => m.CreatoAt);
            if (after.HasValue)
                query = query.Where(m => m.CreatoAt > after.Value);

            return Task.FromResult<IEnumerable<Messaggio>>(query.TakeLast(Math.Clamp(limit, 1, 500)).ToList());
        }
    }

    public Task<Messaggio?> GetUltimoByConversazioneIdAsync(Guid conversazioneId)
    {
        lock (db.Gate)
            return Task.FromResult(db.Messaggi
                .Where(m => m.ConversazioneId == conversazioneId)
                .OrderByDescending(m => m.CreatoAt)
                .FirstOrDefault());
    }

    public Task<Messaggio?> CreateAsync(Messaggio messaggio)
    {
        lock (db.Gate)
        {
            db.Messaggi.Add(messaggio);
            return Task.FromResult<Messaggio?>(messaggio);
        }
    }

    public Task<int> CountNonLettiAsync(Guid conversazioneId, Guid esclusoMittenteId)
    {
        lock (db.Gate)
            return Task.FromResult(db.Messaggi.Count(m =>
                m.ConversazioneId == conversazioneId &&
                m.MittenteId != esclusoMittenteId &&
                m.LettoAt is null));
    }

    public Task<bool> SegnaLettiAsync(Guid conversazioneId, Guid lettoreId, DateTimeOffset lettoAt)
    {
        lock (db.Gate)
        {
            foreach (var messaggio in db.Messaggi.Where(m =>
                         m.ConversazioneId == conversazioneId &&
                         m.MittenteId != lettoreId &&
                         m.LettoAt is null))
            {
                messaggio.LettoAt = lettoAt;
            }

            return Task.FromResult(true);
        }
    }
}
