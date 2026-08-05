using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MyCareHub.Backend.Entities;

[Table("messaggi")]
public class Messaggio : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("conversazione_id")]
    public Guid ConversazioneId { get; set; }

    [Column("mittente_id")]
    public Guid MittenteId { get; set; }

    [Column("contenuto")]
    public string Contenuto { get; set; } = string.Empty;

    [Column("creato_at")]
    public DateTimeOffset CreatoAt { get; set; }

    [Column("letto_at")]
    public DateTimeOffset? LettoAt { get; set; }
}
