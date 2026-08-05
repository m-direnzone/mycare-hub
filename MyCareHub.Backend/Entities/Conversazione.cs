using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MyCareHub.Backend.Entities;

[Table("conversazioni")]
public class Conversazione : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("paziente_id")]
    public Guid PazienteId { get; set; }

    [Column("medico_id")]
    public Guid MedicoId { get; set; }

    [Column("creato_at")]
    public DateTimeOffset CreatoAt { get; set; }

    [Column("aggiornato_at")]
    public DateTimeOffset AggiornatoAt { get; set; }

    [Column("ultimo_messaggio_at")]
    public DateTimeOffset? UltimoMessaggioAt { get; set; }
}
