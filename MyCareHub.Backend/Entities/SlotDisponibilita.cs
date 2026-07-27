using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace MyCareHub.Backend.Entities;

[Table("slot_disponibilita")]
public class SlotDisponibilita : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("medico_id")]
    public Guid MedicoId { get; set; }

    [Column("data_ora_inizio")]
    public DateTimeOffset DataOraInizio { get; set; }

    [Column("data_ora_fine")]
    public DateTimeOffset DataOraFine { get; set; }

    [Column("is_prenotato")]
    public bool IsPrenotato { get; set; }

    [Column("creato_at")]
    public DateTimeOffset CreatoAt { get; set; }
}
