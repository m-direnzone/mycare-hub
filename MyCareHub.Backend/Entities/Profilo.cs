using MyCareHub.Backend.Entities.Enums;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MyCareHub.Backend.Entities;

[Table("profili")]
public class Profilo : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("nome")]
    public string Nome { get; set; } = string.Empty;

    [Column("cognome")]
    public string Cognome { get; set; } = string.Empty;

    [Column("codice_fiscale")]
    public string CodiceFiscale { get; set; } = string.Empty;

    [Column("data_nascita")]
    public DateOnly DataNascita { get; set; }

    [Column("telefono")]
    public string? Telefono { get; set; }

    [Column("ruolo")]
    public RuoloUtente Ruolo { get; set; } = RuoloUtente.Paziente;

    [Column("creato_at")]
    public DateTimeOffset CreatoAt { get; set; }

    [Column("aggiornato_at")]
    public DateTimeOffset AggiornatoAt { get; set; }
}
