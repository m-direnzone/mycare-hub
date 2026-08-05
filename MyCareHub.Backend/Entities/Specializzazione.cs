using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MyCareHub.Backend.Entities;

[Table("specializzazioni")]
public class Specializzazione : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Column("creato_at")]
    public DateTimeOffset CreatoAt { get; set; }

    /// <summary>
    /// Soft delete: valorizzato quando la voce è disabilitata ma resta nello storico.
    /// </summary>
    [Column("deleted_at")]
    [Newtonsoft.Json.JsonProperty(NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore)]
    public DateTimeOffset? DeletedAt { get; set; }
}
