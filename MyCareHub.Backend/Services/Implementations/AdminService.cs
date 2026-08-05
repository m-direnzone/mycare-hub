using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Models.DTOs.Admin;
using MyCareHub.Backend.Repositories.Interfaces;
using MyCareHub.Backend.Services.Interfaces;

namespace MyCareHub.Backend.Services.Implementations;

public class AdminService : IAdminService
{
    private readonly IProfiloRepository _profiloRepository;
    private readonly IMedicoRepository _medicoRepository;
    private readonly IPrenotazioneRepository _prenotazioneRepository;
    private readonly ISlotDisponibilitaRepository _slotRepository;
    private readonly ISpecializzazioneRepository _specializzazioneRepository;
    private readonly IAuditLogRepository _auditLogRepository;

    public AdminService(
        IProfiloRepository profiloRepository,
        IMedicoRepository medicoRepository,
        IPrenotazioneRepository prenotazioneRepository,
        ISlotDisponibilitaRepository slotRepository,
        ISpecializzazioneRepository specializzazioneRepository,
        IAuditLogRepository auditLogRepository)
    {
        _profiloRepository = profiloRepository;
        _medicoRepository = medicoRepository;
        _prenotazioneRepository = prenotazioneRepository;
        _slotRepository = slotRepository;
        _specializzazioneRepository = specializzazioneRepository;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<AdminKpiDto> GetKpiAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var inizioMese = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var fineMese = inizioMese.AddMonths(1).AddTicks(-1);
        var inizioMesePrec = inizioMese.AddMonths(-1);
        var fineMesePrec = inizioMese.AddTicks(-1);

        var pazientiTask = _profiloRepository.GetByRuoloAsync(RuoloUtente.Paziente);
        var mediciTask = _medicoRepository.GetTuttiMediciAsync();
        var prenotazioniTask = _prenotazioneRepository.GetAllAsync(inizioMese, fineMese);
        var dettagliTask = _medicoRepository.GetAllDettagliAsync();
        await Task.WhenAll(pazientiTask, mediciTask, prenotazioniTask, dettagliTask);

        var pazienti = (await pazientiTask).ToList();
        var pazientiMese = pazienti.Count(p => p.CreatoAt >= inizioMese && p.CreatoAt <= fineMese);
        var pazientiMesePrec = pazienti.Count(p => p.CreatoAt >= inizioMesePrec && p.CreatoAt <= fineMesePrec);

        decimal delta = 0;
        if (pazientiMesePrec > 0)
            delta = Math.Round((pazientiMese - pazientiMesePrec) / (decimal)pazientiMesePrec * 100m, 1);
        else if (pazientiMese > 0)
            delta = 100m;

        var medici = (await mediciTask).ToList();

        var mediciAttiviPasswordOk = 0;
        var mediciInAttesa = 0;
        foreach (var (_, dettagli) in medici)
        {
            if (!dettagli.Attivo)
                continue;

            if (dettagli.PasswordCambiata)
                mediciAttiviPasswordOk++;
            else
                mediciInAttesa++;
        }

        var prenotazioniMese = (await prenotazioniTask).ToList();
        var dettagliMedici = (await dettagliTask).ToDictionary(d => d.MedicoId);

        var completate = 0;
        var cancellate = 0;
        decimal volume = 0;

        foreach (var p in prenotazioniMese)
        {
            if (p.Stato == StatoPrenotazione.Completata)
            {
                completate++;
                if (dettagliMedici.TryGetValue(p.MedicoId, out var dett))
                    volume += dett.TariffaVisita;
            }
            else if (p.Stato == StatoPrenotazione.Cancellata)
            {
                cancellate++;
            }
        }

        var denominatore = completate + cancellate;
        var tasso = denominatore > 0
            ? Math.Round(completate / (decimal)denominatore * 100m, 1)
            : 0m;

        return new AdminKpiDto
        {
            TotalePazientiRegistrati = pazienti.Count,
            PazientiNuoviMeseCorrente = pazientiMese,
            PazientiNuoviMesePrecedente = pazientiMesePrec,
            DeltaCrescitaMensilePercentuale = delta,
            TotaleMediciAttivi = mediciAttiviPasswordOk,
            MediciInAttesaAttivazione = mediciInAttesa,
            VisitePrenotateMeseCorrente = prenotazioniMese.Count,
            VisiteCompletateMeseCorrente = completate,
            VisiteCancellateOSaltateMeseCorrente = cancellate,
            TassoCompletamentoPercentuale = tasso,
            VolumeAffariIntermediatoMese = volume
        };
    }

    public async Task<IEnumerable<AdminMedicoDto>> GetMediciAsync(
        CancellationToken cancellationToken = default)
    {
        var medici = (await _medicoRepository.GetTuttiMediciAsync()).ToList();

        return medici.Select(m => new AdminMedicoDto
        {
            Id = m.Profilo.Id,
            Email = null,
            Nome = m.Profilo.Nome,
            Cognome = m.Profilo.Cognome,
            CodiceFiscale = m.Profilo.CodiceFiscale,
            DataNascita = m.Profilo.DataNascita,
            Telefono = m.Profilo.Telefono,
            Specializzazione = m.Dettagli.Specializzazione,
            NumeroIscrizioneAlbo = m.Dettagli.NumeroIscrizioneAlbo,
            Biografia = m.Dettagli.Biografia,
            TariffaVisita = m.Dettagli.TariffaVisita,
            Attivo = m.Dettagli.Attivo,
            PasswordCambiata = m.Dettagli.PasswordCambiata,
            CreatoAt = m.Profilo.CreatoAt
        });
    }

    public async Task<bool> AggiornaMedicoAsync(
        Guid medicoId,
        AdminMedicoUpdateDto dto,
        Guid attoreId,
        string attoreNome)
    {
        var profilo = await _profiloRepository.GetByIdAsync(medicoId);
        if (profilo is null || profilo.Ruolo != RuoloUtente.Medico)
            return false;

        var dettagli = await _medicoRepository.GetDettagliByIdAsync(medicoId);
        if (dettagli is null)
            return false;

        var wasAttivo = dettagli.Attivo;
        if (wasAttivo && !dto.Attivo)
            await EnsureMedicoSenzaVisiteFutureAsync(medicoId);

        profilo.Nome = dto.Nome.Trim();
        profilo.Cognome = dto.Cognome.Trim();
        profilo.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
        await _profiloRepository.UpdateAsync(profilo);

        dettagli.Specializzazione = dto.Specializzazione.Trim();
        dettagli.NumeroIscrizioneAlbo = dto.NumeroIscrizioneAlbo.Trim();
        dettagli.Biografia = string.IsNullOrWhiteSpace(dto.Biografia) ? null : dto.Biografia.Trim();
        dettagli.TariffaVisita = dto.TariffaVisita;
        dettagli.Attivo = dto.Attivo;
        try
        {
            var ok = await _medicoRepository.UpdateMedicoDettagliAsync(dettagli);
            if (!ok)
                return false;
        }
        catch (Exception ex)
        {
            RethrowConstraintViolation(ex,
                "Impossibile disabilitare il medico: ha almeno una prenotazione confermata in futuro.");
            throw;
        }

        await LogAsync(
            attoreId,
            attoreNome,
            "Modifica medico",
            "medico",
            medicoId,
            $"L'amministratore {attoreNome} ha modificato il medico {profilo.Nome} {profilo.Cognome}.");

        if (wasAttivo != dto.Attivo)
        {
            await LogAsync(
                attoreId,
                attoreNome,
                dto.Attivo ? "Riattivazione medico" : "Disabilitazione medico",
                "medico",
                medicoId,
                $"L'amministratore {attoreNome} ha {(dto.Attivo ? "abilitato" : "disabilitato")} il medico {profilo.Nome} {profilo.Cognome}.");
        }

        return true;
    }

    public async Task<bool> SoftDeleteMedicoAsync(Guid medicoId, Guid attoreId, string attoreNome)
    {
        var profilo = await _profiloRepository.GetByIdAsync(medicoId);
        if (profilo is null || profilo.Ruolo != RuoloUtente.Medico)
            return false;

        var dettagli = await _medicoRepository.GetDettagliByIdAsync(medicoId);
        if (dettagli is null)
            return false;

        if (!dettagli.Attivo)
            return true;

        await EnsureMedicoSenzaVisiteFutureAsync(medicoId);

        dettagli.Attivo = false;
        try
        {
            var ok = await _medicoRepository.UpdateMedicoDettagliAsync(dettagli);
            if (!ok)
                return false;
        }
        catch (Exception ex)
        {
            RethrowConstraintViolation(ex,
                "Impossibile disabilitare il medico: ha almeno una prenotazione confermata in futuro.");
            throw;
        }

        await LogAsync(
            attoreId,
            attoreNome,
            "Disabilitazione medico",
            "medico",
            medicoId,
            $"L'amministratore {attoreNome} ha disabilitato il medico {profilo.Nome} {profilo.Cognome}.");

        return true;
    }

    public async Task<bool> RiattivaMedicoAsync(Guid medicoId, Guid attoreId, string attoreNome)
    {
        var profilo = await _profiloRepository.GetByIdAsync(medicoId);
        if (profilo is null || profilo.Ruolo != RuoloUtente.Medico)
            return false;

        var dettagli = await _medicoRepository.GetDettagliByIdAsync(medicoId);
        if (dettagli is null)
            return false;

        if (dettagli.Attivo)
            return true;

        dettagli.Attivo = true;
        var ok = await _medicoRepository.UpdateMedicoDettagliAsync(dettagli);
        if (!ok)
            return false;

        await LogAsync(
            attoreId,
            attoreNome,
            "Riattivazione medico",
            "medico",
            medicoId,
            $"L'amministratore {attoreNome} ha abilitato il medico {profilo.Nome} {profilo.Cognome}.");

        return true;
    }

    public async Task<IEnumerable<AdminPazienteDto>> GetPazientiAsync()
    {
        var pazienti = await _profiloRepository.GetByRuoloAsync(RuoloUtente.Paziente);
        return pazienti.Select(p => new AdminPazienteDto
        {
            Id = p.Id,
            Nome = p.Nome,
            Cognome = p.Cognome,
            CodiceFiscale = p.CodiceFiscale,
            DataNascita = p.DataNascita,
            Telefono = p.Telefono,
            CreatoAt = p.CreatoAt
        });
    }

    public async Task<IEnumerable<AdminPrenotazioneDto>> GetPrenotazioniAsync(
        int? anno = null,
        int? mese = null,
        int? giorno = null)
    {
        DateTimeOffset? inizio = null;
        DateTimeOffset? fine = null;

        if (anno is > 0)
        {
            var y = anno.Value;
            var m = mese is >= 1 and <= 12 ? mese.Value : 1;
            if (giorno is >= 1 and <= 31 && mese is >= 1 and <= 12)
            {
                var d = Math.Min(giorno.Value, DateTime.DaysInMonth(y, m));
                inizio = new DateTimeOffset(y, m, d, 0, 0, 0, TimeSpan.Zero);
                fine = inizio.Value.AddDays(1).AddTicks(-1);
            }
            else if (mese is >= 1 and <= 12)
            {
                inizio = new DateTimeOffset(y, m, 1, 0, 0, 0, TimeSpan.Zero);
                fine = inizio.Value.AddMonths(1).AddTicks(-1);
            }
            else
            {
                inizio = new DateTimeOffset(y, 1, 1, 0, 0, 0, TimeSpan.Zero);
                fine = inizio.Value.AddYears(1).AddTicks(-1);
            }
        }

        var prenotazioni = (await _prenotazioneRepository.GetAllAsync(inizio, fine)).ToList();
        if (prenotazioni.Count == 0)
            return [];

        var slotsTask = _slotRepository.GetByIdsAsync(prenotazioni.Select(p => p.SlotId));
        var profiloIds = prenotazioni
            .SelectMany(p => new[] { p.MedicoId, p.PazienteId })
            .Distinct();
        var profiliTask = _profiloRepository.GetByIdsAsync(profiloIds);
        var dettagliTask = _medicoRepository.GetAllDettagliAsync();
        await Task.WhenAll(slotsTask, profiliTask, dettagliTask);

        var slots = (await slotsTask)
            .ToDictionary(s => s.Id);
        var profili = (await profiliTask)
            .ToDictionary(p => p.Id);
        var dettagli = (await dettagliTask)
            .ToDictionary(d => d.MedicoId);

        return prenotazioni
            .Select(p =>
            {
                slots.TryGetValue(p.SlotId, out var slot);
                profili.TryGetValue(p.MedicoId, out var medico);
                profili.TryGetValue(p.PazienteId, out var paziente);
                dettagli.TryGetValue(p.MedicoId, out var dett);

                return new AdminPrenotazioneDto
                {
                    Id = p.Id,
                    Stato = p.Stato,
                    DataOraInizio = slot?.DataOraInizio ?? p.CreatoAt,
                    DataOraFine = slot?.DataOraFine ?? p.CreatoAt,
                    MedicoId = p.MedicoId,
                    MedicoNomeCompleto = medico is null ? "—" : $"{medico.Nome} {medico.Cognome}",
                    MedicoSpecializzazione = dett?.Specializzazione ?? "—",
                    PazienteId = p.PazienteId,
                    PazienteNomeCompleto = paziente is null ? "—" : $"{paziente.Nome} {paziente.Cognome}",
                    PazienteCodiceFiscale = paziente?.CodiceFiscale ?? "—",
                    TariffaVisita = dett?.TariffaVisita,
                    CreatoAt = p.CreatoAt
                };
            })
            .OrderByDescending(p => p.DataOraInizio);
    }

    public async Task<IEnumerable<AuditLogDto>> GetAuditLogAsync(int limit = 200)
    {
        var entries = await _auditLogRepository.GetRecentAsync(limit);
        return entries.Select(e => new AuditLogDto
        {
            Id = e.Id,
            AttoreId = e.AttoreId,
            AttoreNome = e.AttoreNome,
            Azione = e.Azione,
            EntitaTipo = e.EntitaTipo,
            EntitaId = e.EntitaId,
            Dettagli = e.Dettagli,
            CreatoAt = e.CreatoAt
        });
    }

    public async Task LogAsync(
        Guid? attoreId,
        string? attoreNome,
        string azione,
        string? entitaTipo = null,
        Guid? entitaId = null,
        string? dettagli = null)
    {
        await _auditLogRepository.CreateAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            AttoreId = attoreId,
            AttoreNome = attoreNome,
            Azione = azione,
            EntitaTipo = entitaTipo,
            EntitaId = entitaId,
            Dettagli = dettagli,
            CreatoAt = DateTimeOffset.UtcNow
        });
    }

    public async Task<IEnumerable<SpecializzazioneDto>> GetSpecializzazioniAsync()
    {
        var items = await _specializzazioneRepository.GetAllAsync();
        return items.Select(s => new SpecializzazioneDto
        {
            Id = s.Id,
            Nome = s.Nome,
            CreatoAt = s.CreatoAt
        });
    }

    public async Task<SpecializzazioneDto> CreaSpecializzazioneAsync(
        SpecializzazioneCreateDto dto,
        Guid attoreId,
        string attoreNome)
    {
        var nome = dto.Nome.Trim();
        var existingAttiva = await _specializzazioneRepository.GetByNomeAsync(nome);
        if (existingAttiva is not null)
            throw new InvalidOperationException("Specializzazione già presente.");

        var softDeleted = await _specializzazioneRepository.GetByNomeIncludingDeletedAsync(nome);
        if (softDeleted is not null && softDeleted.DeletedAt is not null)
        {
            var restored = await _specializzazioneRepository.RestoreAsync(softDeleted.Id);
            if (!restored)
                throw new InvalidOperationException("Impossibile ripristinare la specializzazione.");

            await LogAsync(
                attoreId,
                attoreNome,
                "Ripristino specializzazione",
                "specializzazione",
                softDeleted.Id,
                $"L'amministratore {attoreNome} ha ripristinato la specializzazione «{nome}».");

            return new SpecializzazioneDto
            {
                Id = softDeleted.Id,
                Nome = softDeleted.Nome,
                CreatoAt = softDeleted.CreatoAt
            };
        }

        var created = await _specializzazioneRepository.CreateAsync(new Specializzazione
        {
            Id = Guid.NewGuid(),
            Nome = nome,
            CreatoAt = DateTimeOffset.UtcNow
        });

        if (created is null)
            throw new InvalidOperationException("Impossibile creare la specializzazione.");

        await LogAsync(
            attoreId,
            attoreNome,
            "Aggiunta specializzazione",
            "specializzazione",
            created.Id,
            $"L'amministratore {attoreNome} ha aggiunto la specializzazione «{nome}».");

        return new SpecializzazioneDto
        {
            Id = created.Id,
            Nome = created.Nome,
            CreatoAt = created.CreatoAt
        };
    }

    public async Task<bool> EliminaSpecializzazioneAsync(Guid id, Guid attoreId, string attoreNome)
    {
        var existing = await _specializzazioneRepository.GetByIdAsync(id);
        if (existing is null)
            return false;

        var mediciAttivi = await _medicoRepository.GetAllAttiviAsync();
        var assegnata = mediciAttivi.Any(m =>
            string.Equals(m.Specializzazione, existing.Nome, StringComparison.OrdinalIgnoreCase));
        if (assegnata)
        {
            throw new InvalidOperationException(
                "Impossibile eliminare la specializzazione: è assegnata ad almeno un medico attivo.");
        }

        var ok = false;
        try
        {
            ok = await _specializzazioneRepository.SoftDeleteAsync(id);
        }
        catch (Exception ex)
        {
            RethrowConstraintViolation(ex,
                "Impossibile eliminare la specializzazione: è assegnata ad almeno un medico attivo.");
            throw;
        }

        if (!ok)
            return false;

        await LogAsync(
            attoreId,
            attoreNome,
            "Rimozione specializzazione",
            "specializzazione",
            id,
            $"L'amministratore {attoreNome} ha rimosso la specializzazione «{existing.Nome}».");

        return true;
    }

    private async Task EnsureMedicoSenzaVisiteFutureAsync(Guid medicoId)
    {
        var future = await _prenotazioneRepository.GetByMedicoIdAsync(
            medicoId,
            dataInizio: DateTimeOffset.UtcNow);

        var hasFutureConfermata = future.Any(p => p.Stato == StatoPrenotazione.Confermata);

        if (hasFutureConfermata)
        {
            throw new InvalidOperationException(
                "Impossibile disabilitare il medico: ha almeno una prenotazione confermata in futuro.");
        }
    }

    private static void RethrowConstraintViolation(Exception ex, string fallbackMessage)
    {
        var text = ex.Message ?? string.Empty;
        if (ex.InnerException is not null)
            text = $"{text} {ex.InnerException.Message}";

        if (text.Contains("prenotazione confermata", StringComparison.OrdinalIgnoreCase)
            || text.Contains("visita prenotata in futuro", StringComparison.OrdinalIgnoreCase)
            || text.Contains("medico attivo", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Hard delete non consentito", StringComparison.OrdinalIgnoreCase)
            || text.Contains("P0001", StringComparison.OrdinalIgnoreCase))
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                text,
                @"Impossibile [^""]+|Hard delete non consentito[^""]*");
            throw new InvalidOperationException(
                match.Success ? match.Value.Trim() : fallbackMessage,
                ex);
        }
    }
}
