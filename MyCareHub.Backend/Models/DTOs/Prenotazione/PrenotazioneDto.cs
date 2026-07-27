using MyCareHub.Backend.Entities.Enums;

namespace MyCareHub.Backend.Models.DTOs.Prenotazione;

/// <summary>
/// Vista denormalizzata di una prenotazione per elenchi/dettaglio frontend
/// (es. "Le mie visite"), evitando chiamate ricorsive.
/// </summary>
public class PrenotazioneDto
{
    public Guid Id { get; set; }
    public Guid SlotId { get; set; }
    public StatoPrenotazione Stato { get; set; }
    public string? NotePaziente { get; set; }
    public string? NoteMediche { get; set; }
    public DateTimeOffset CreatoAt { get; set; }
    public DateTimeOffset AggiornatoAt { get; set; }

    // Slot temporale aggregato
    public DateTimeOffset DataOraInizio { get; set; }
    public DateTimeOffset DataOraFine { get; set; }

    // Anagrafica medico (vista paziente)
    public Guid MedicoId { get; set; }
    public string MedicoNomeCompleto { get; set; } = null!;
    public string MedicoSpecializzazione { get; set; } = null!;

    // Anagrafica paziente (vista medico)
    public Guid PazienteId { get; set; }
    public string PazienteNomeCompleto { get; set; } = null!;
    public string PazienteCodiceFiscale { get; set; } = null!;
}
