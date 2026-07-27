using System.ComponentModel.DataAnnotations;

namespace MyCareHub.Backend.Models.DTOs.Prenotazione;

/// <summary>
/// Payload inviato dal medico per chiudere la visita, salvare il referto testuale
/// e portare lo stato a Completata.
/// </summary>
public class PrenotazioneConclusioneDto
{
    [Required(ErrorMessage = "Il resoconto clinico della visita è obbligatorio.")]
    [MinLength(10, ErrorMessage = "Le note mediche devono essere dettagliate (almeno 10 caratteri).")]
    [StringLength(5000, ErrorMessage = "Le note mediche non possono superare i 5000 caratteri.")]
    public string NoteMediche { get; set; } = null!;
}
