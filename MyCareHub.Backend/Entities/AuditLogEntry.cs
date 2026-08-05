using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MyCareHub.Backend.Entities;

[Table("audit_log")]
public class AuditLogEntry : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("attore_id")]
    public Guid? AttoreId { get; set; }

    [Column("attore_nome")]
    public string? AttoreNome { get; set; }

    [Column("azione")]
    public string Azione { get; set; } = string.Empty;

    [Column("entita_tipo")]
    public string? EntitaTipo { get; set; }

    [Column("entita_id")]
    public Guid? EntitaId { get; set; }

    [Column("dettagli")]
    public string? Dettagli { get; set; }

    [Column("creato_at")]
    public DateTimeOffset CreatoAt { get; set; }
}
