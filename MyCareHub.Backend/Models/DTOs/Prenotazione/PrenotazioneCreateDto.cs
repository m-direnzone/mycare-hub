using System.ComponentModel.DataAnnotations;

namespace MyCareHub.Backend.Models.DTOs.Prenotazione;

/// <summary>
/// Payload inviato dal paziente per bloccare uno slot e confermare la visita.
/// L'id del paziente viene estratto dal JWT lato backend (non accettato dal client).
/// </summary>
public class PrenotazioneCreateDto
{
    [Required(ErrorMessage = "Lo slot orario selezionato non è valido.")]
    public Guid SlotId { get; set; }

    [Required(ErrorMessage = "L'ID del medico è obbligatorio.")]
    public Guid MedicoId { get; set; }

    [StringLength(500, ErrorMessage = "Le note non possono superare i 500 caratteri.")]
    public string? NotePaziente { get; set; }
}
