using MyCareHub.Backend.Entities;
using MyCareHub.Backend.Entities.Enums;
using MyCareHub.Backend.Models.DTOs.Slot;
using MyCareHub.Backend.Repositories.Interfaces;
using MyCareHub.Backend.Services.Interfaces;

namespace MyCareHub.Backend.Services.Implementations;

public class SlotService : ISlotService
{
    private readonly ISlotDisponibilitaRepository _slotRepository;
    private readonly IMedicoRepository _medicoRepository;
    private readonly IProfiloRepository _profiloRepository;

    public SlotService(
        ISlotDisponibilitaRepository slotRepository,
        IMedicoRepository medicoRepository,
        IProfiloRepository profiloRepository)
    {
        _slotRepository = slotRepository;
        _medicoRepository = medicoRepository;
        _profiloRepository = profiloRepository;
    }

    public async Task<IEnumerable<SlotDto>> GetSlotDisponibiliMedicoAsync(
        Guid medicoId,
        DateTimeOffset? dataInizio = null,
        DateTimeOffset? dataFine = null)
    {
        var inizio = dataInizio ?? DateTimeOffset.UtcNow;

        var slots = await _slotRepository.GetByMedicoIdAsync(
            medicoId,
            inizio,
            dataFine,
            isPrenotato: false);

        return slots.Select(MapSlot);
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

        // Intervalli invalidi o sovrapposti tra loro
        var ordinati = dto.SlotRichiesti
            .OrderBy(s => s.DataOraInizio)
            .ToList();

        for (var i = 0; i < ordinati.Count; i++)
        {
            var corrente = ordinati[i];
            if (corrente.DataOraFine <= corrente.DataOraInizio)
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

        var now = DateTimeOffset.UtcNow;
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

    private static SlotDto MapSlot(SlotDisponibilita slot) => new()
    {
        Id = slot.Id,
        MedicoId = slot.MedicoId,
        DataOraInizio = slot.DataOraInizio,
        DataOraFine = slot.DataOraFine,
        IsPrenotato = slot.IsPrenotato
    };
}
