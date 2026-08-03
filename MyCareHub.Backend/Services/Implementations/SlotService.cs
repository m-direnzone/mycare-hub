using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Models.DTOs.Slot;
using MyCareHub.Backend.Repositories.Interfaces;
using MyCareHub.Backend.Services.Interfaces;

namespace MyCareHub.Backend.Services.Implementations;

public class SlotService : ISlotService
{
    public const int DurataSlotMinuti = 30;

    private static readonly (TimeOnly Start, TimeOnly End)[] FasceOrarie =
    [
        (new TimeOnly(7, 0), new TimeOnly(13, 0)),
        (new TimeOnly(14, 0), new TimeOnly(18, 0))
    ];

    private static readonly TimeZoneInfo TzItalia = ResolveItalyTimeZone();

    private readonly ISlotDisponibilitaRepository _slotRepository;
    private readonly IMedicoRepository _medicoRepository;
    private readonly IProfiloRepository _profiloRepository;
    private readonly IPrenotazioneRepository _prenotazioneRepository;

    public SlotService(
        ISlotDisponibilitaRepository slotRepository,
        IMedicoRepository medicoRepository,
        IProfiloRepository profiloRepository,
        IPrenotazioneRepository prenotazioneRepository)
    {
        _slotRepository = slotRepository;
        _medicoRepository = medicoRepository;
        _profiloRepository = profiloRepository;
        _prenotazioneRepository = prenotazioneRepository;
    }

    public async Task<IEnumerable<SlotDto>> GetSlotDisponibiliMedicoAsync(
        Guid medicoId,
        Guid? pazienteId = null,
        DateTimeOffset? dataInizio = null,
        DateTimeOffset? dataFine = null)
    {
        var dettagli = await _medicoRepository.GetDettagliByIdAsync(medicoId);
        if (dettagli is null || !dettagli.Attivo)
            return [];

        var nowUtc = DateTimeOffset.UtcNow;
        var oggiLocale = TimeZoneInfo.ConvertTime(nowUtc, TzItalia).Date;

        var inizioLocale = dataInizio.HasValue
            ? TimeZoneInfo.ConvertTime(dataInizio.Value, TzItalia).Date
            : oggiLocale;

        if (inizioLocale < oggiLocale)
            inizioLocale = oggiLocale;

        var fineLocale = dataFine.HasValue
            ? TimeZoneInfo.ConvertTime(dataFine.Value, TzItalia).Date
            : inizioLocale.AddDays(60);

        if (fineLocale < inizioLocale)
            return [];

        var rangeStartUtc = ToUtc(inizioLocale, TimeOnly.MinValue);
        var rangeEndUtc = ToUtc(fineLocale.AddDays(1), TimeOnly.MinValue).AddTicks(-1);

        var slotMedico = (await _slotRepository.GetByMedicoIdAsync(
            medicoId,
            rangeStartUtc,
            rangeEndUtc)).ToList();

        var intervalliOccupatiPaziente = new List<(DateTimeOffset Inizio, DateTimeOffset Fine)>();
        if (pazienteId.HasValue)
        {
            intervalliOccupatiPaziente = await GetIntervalliOccupatiPazienteAsync(
                pazienteId.Value,
                rangeStartUtc,
                rangeEndUtc);
        }

        // Solo slot effettivamente generati dal medico in agenda (nessuna fascia virtuale).
        return slotMedico
            .Where(s => !s.IsPrenotato)
            .Where(s => s.DataOraInizio >= nowUtc)
            .Where(s => !intervalliOccupatiPaziente.Any(o =>
                Overlaps(s.DataOraInizio, s.DataOraFine, o.Inizio, o.Fine)))
            .OrderBy(s => s.DataOraInizio)
            .Select(MapSlot);
    }

    public async Task<IEnumerable<SlotDto>> GetAgendaMedicoAsync(
        Guid medicoId,
        DateTimeOffset? dataInizio = null,
        DateTimeOffset? dataFine = null)
    {
        var inizio = dataInizio ?? DateTimeOffset.UtcNow.AddDays(-7);

        var slots = await _slotRepository.GetByMedicoIdAsync(
            medicoId,
            inizio,
            dataFine);

        return slots.Select(MapSlot);
    }

    public async Task<bool> GeneraAgendaBulkAsync(SlotCreationBulkDto dto)
    {
        if (dto.SlotRichiesti.Count == 0)
            return false;

        var profilo = await _profiloRepository.GetByIdAsync(dto.MedicoId);
        if (profilo is null || profilo.Ruolo != RuoloUtente.Medico)
            return false;

        var dettagli = await _medicoRepository.GetDettagliByIdAsync(dto.MedicoId);
        if (dettagli is null || !dettagli.Attivo)
            return false;

        var ordinati = dto.SlotRichiesti
            .OrderBy(s => s.DataOraInizio)
            .ToList();

        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < ordinati.Count; i++)
        {
            var corrente = ordinati[i];
            if (corrente.DataOraFine <= corrente.DataOraInizio)
                return false;

            // Nessuno slot nel passato.
            if (corrente.DataOraInizio < now)
                return false;

            if (i > 0 && corrente.DataOraInizio < ordinati[i - 1].DataOraFine)
                return false;
        }

        var rangeInizio = ordinati.First().DataOraInizio;
        var rangeFine = ordinati.Last().DataOraFine;
        var esistenti = (await _slotRepository.GetByMedicoIdAsync(
            dto.MedicoId,
            rangeInizio,
            rangeFine)).ToList();

        foreach (var richiesto in ordinati)
        {
            var overlap = esistenti.Any(e =>
                richiesto.DataOraInizio < e.DataOraFine &&
                richiesto.DataOraFine > e.DataOraInizio);

            if (overlap)
                return false;
        }

        var daCreare = ordinati.Select(s => new SlotDisponibilita
        {
            Id = Guid.NewGuid(),
            MedicoId = dto.MedicoId,
            DataOraInizio = s.DataOraInizio,
            DataOraFine = s.DataOraFine,
            IsPrenotato = false,
            CreatoAt = now
        });

        return await _slotRepository.CreateBulkAsync(daCreare);
    }

    public async Task<bool> EliminaSlotAsync(Guid slotId, Guid medicoId)
    {
        var slot = await _slotRepository.GetByIdAsync(slotId);
        if (slot is null || slot.MedicoId != medicoId || slot.IsPrenotato)
            return false;

        return await _slotRepository.DeleteAsync(slotId);
    }

    private async Task<List<(DateTimeOffset Inizio, DateTimeOffset Fine)>> GetIntervalliOccupatiPazienteAsync(
        Guid pazienteId,
        DateTimeOffset rangeStartUtc,
        DateTimeOffset rangeEndUtc)
    {
        var prenotazioni = (await _prenotazioneRepository.GetByPazienteIdAsync(pazienteId))
            .Where(p => p.Stato != StatoPrenotazione.Cancellata)
            .ToList();

        var result = new List<(DateTimeOffset Inizio, DateTimeOffset Fine)>();

        foreach (var prenotazione in prenotazioni)
        {
            var slot = await _slotRepository.GetByIdAsync(prenotazione.SlotId);
            if (slot is null)
                continue;

            if (slot.DataOraFine <= rangeStartUtc || slot.DataOraInizio > rangeEndUtc)
                continue;

            result.Add((slot.DataOraInizio, slot.DataOraFine));
        }

        return result;
    }

    internal static bool IsFasciaStandard(DateTimeOffset inizio, DateTimeOffset fine)
    {
        var durata = (fine - inizio).TotalMinutes;
        if (Math.Abs(durata - DurataSlotMinuti) > 0.5)
            return false;

        var locale = TimeZoneInfo.ConvertTime(inizio, TzItalia);
        var time = new TimeOnly(locale.Hour, locale.Minute);
        var endTime = time.AddMinutes(DurataSlotMinuti);

        foreach (var (fasciaInizio, fasciaFine) in FasceOrarie)
        {
            if (time >= fasciaInizio && endTime <= fasciaFine)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Allinea l'istante alla griglia da 30 minuti nel fuso Italia.
    /// </summary>
    internal static DateTimeOffset NormalizeInizioFascia(DateTimeOffset inizio)
    {
        var locale = TimeZoneInfo.ConvertTime(inizio, TzItalia);
        var minutes = locale.Minute >= 30 ? 30 : 0;
        var normalizedLocal = new DateTime(
            locale.Year,
            locale.Month,
            locale.Day,
            locale.Hour,
            minutes,
            0,
            DateTimeKind.Unspecified);
        return ToUtc(normalizedLocal.Date, TimeOnly.FromDateTime(normalizedLocal));
    }

    internal static DateTimeOffset ToUtc(DateTime localDate, TimeOnly localTime)
    {
        var unspecified = DateTime.SpecifyKind(localDate.Add(localTime.ToTimeSpan()), DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, TzItalia.GetUtcOffset(unspecified));
    }

    private static bool Overlaps(
        DateTimeOffset aStart,
        DateTimeOffset aEnd,
        DateTimeOffset bStart,
        DateTimeOffset bEnd) =>
        aStart < bEnd && aEnd > bStart;

    private static SlotDto MapSlot(SlotDisponibilita slot) => new()
    {
        Id = slot.Id,
        MedicoId = slot.MedicoId,
        DataOraInizio = slot.DataOraInizio,
        DataOraFine = slot.DataOraFine,
        IsPrenotato = slot.IsPrenotato
    };

    private static TimeZoneInfo ResolveItalyTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "W. Europe Standard Time" : "Europe/Rome");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
