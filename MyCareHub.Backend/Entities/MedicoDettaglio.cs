using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MyCareHub.Backend.Entities;

[Table("medici_dettagli")]
public class MedicoDettaglio : BaseModel
{
    [PrimaryKey("medico_id", false)]
    public Guid MedicoId { get; set; }

    [Column("specializzazione")]
    public string Specializzazione { get; set; } = string.Empty;

    [Column("numero_iscrizione_albo")]
    public string NumeroIscrizioneAlbo { get; set; } = string.Empty;

    [Column("biografia")]
    public string? Biografia { get; set; }

    [Column("tariffa_visita")]
    public decimal TariffaVisita { get; set; }

    [Column("attivo")]
    public bool Attivo { get; set; } = true;
}
