namespace MyCareHub.Backend.Models.DTOs.Slot;

/// <summary>
/// Slot orario restituito al frontend (calendario / ricerca disponibilità).
/// </summary>
public class SlotDto
{
    public Guid Id { get; set; }
    public Guid MedicoId { get; set; }
    public DateTimeOffset DataOraInizio { get; set; }
    public DateTimeOffset DataOraFine { get; set; }
    public bool IsPrenotato { get; set; }
}
