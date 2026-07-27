using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MyCareHub.Backend.Entities;

[Table("referti_medici")]
public class RefertoMedico : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("prenotazione_id")]
    public Guid? PrenotazioneId { get; set; }

    [Column("paziente_id")]
    public Guid PazienteId { get; set; }

    [Column("medico_id")]
    public Guid MedicoId { get; set; }

    [Column("titolo")]
    public string Titolo { get; set; } = string.Empty;

    [Column("descrizione")]
    public string? Descrizione { get; set; }

    [Column("file_path")]
    public string FilePath { get; set; } = string.Empty;

    [Column("data_emissione")]
    public DateOnly DataEmissione { get; set; }

    [Column("creato_at")]
    public DateTimeOffset CreatoAt { get; set; }
}
