using System.ComponentModel.DataAnnotations;

namespace MyCareHub.Backend.Models.DTOs.Chat;

public class ConversazioneDto
{
    public Guid Id { get; set; }
    public Guid PazienteId { get; set; }
    public Guid MedicoId { get; set; }
    public string ContattoNomeCompleto { get; set; } = null!;
    public string? ContattoSottoTitolo { get; set; }
    public DateTimeOffset CreatoAt { get; set; }
    public DateTimeOffset AggiornatoAt { get; set; }
    public DateTimeOffset? UltimoMessaggioAt { get; set; }
    public string? AnteprimaUltimoMessaggio { get; set; }
    public int MessaggiNonLetti { get; set; }
}

public class MessaggioDto
{
    public Guid Id { get; set; }
    public Guid ConversazioneId { get; set; }
    public Guid MittenteId { get; set; }
    public string Contenuto { get; set; } = null!;
    public DateTimeOffset CreatoAt { get; set; }
    public DateTimeOffset? LettoAt { get; set; }
    public bool IsMio { get; set; }
}

public class ConversazioneCreateDto
{
    /// <summary>
    /// Controparte della chat: medico_id se chiamante è paziente, paziente_id se chiamante è medico.
    /// </summary>
    [Required]
    public Guid ContattoId { get; set; }
}

public class MessaggioCreateDto
{
    [Required]
    [MinLength(1)]
    [MaxLength(4000)]
    public string Contenuto { get; set; } = null!;
}

public class ChatContattoDto
{
    public Guid Id { get; set; }
    public string NomeCompleto { get; set; } = null!;
    public string? SottoTitolo { get; set; }
    public Guid? ConversazioneId { get; set; }
}
