using MyCareHub.Backend.Entities.Enums;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MyCareHub.Backend.Entities;

[Table("prenotazioni")]
public class Prenotazione : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("paziente_id")]
    public Guid PazienteId { get; set; }

    [Column("medico_id")]
    public Guid MedicoId { get; set; }

    [Column("slot_id")]
    public Guid SlotId { get; set; }

    [Column("stato")]
    public StatoPrenotazione Stato { get; set; } = StatoPrenotazione.InAttesa;

    [Column("note_paziente")]
    public string? NotePaziente { get; set; }

    [Column("note_mediche")]
    public string? NoteMediche { get; set; }

    [Column("creato_at")]
    public DateTimeOffset CreatoAt { get; set; }

    [Column("aggiornato_at")]
    public DateTimeOffset AggiornatoAt { get; set; }
}
