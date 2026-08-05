using System.ComponentModel.DataAnnotations;
using MyCareHub.Backend.Entities.Enums;

namespace MyCareHub.Backend.Models.DTOs.Admin;

public class AdminKpiDto
{
    public int TotalePazientiRegistrati { get; set; }
    public int PazientiNuoviMeseCorrente { get; set; }
    public int PazientiNuoviMesePrecedente { get; set; }
    public decimal DeltaCrescitaMensilePercentuale { get; set; }

    public int TotaleMediciAttivi { get; set; }
    public int MediciInAttesaAttivazione { get; set; }

    public int VisitePrenotateMeseCorrente { get; set; }
    public int VisiteCompletateMeseCorrente { get; set; }
    public int VisiteCancellateOSaltateMeseCorrente { get; set; }
    public decimal TassoCompletamentoPercentuale { get; set; }

    public decimal VolumeAffariIntermediatoMese { get; set; }
}

public class AdminMedicoDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string Nome { get; set; } = null!;
    public string Cognome { get; set; } = null!;
    public string CodiceFiscale { get; set; } = null!;
    public DateOnly DataNascita { get; set; }
    public string? Telefono { get; set; }
    public string Specializzazione { get; set; } = null!;
    public string NumeroIscrizioneAlbo { get; set; } = null!;
    public string? Biografia { get; set; }
    public decimal TariffaVisita { get; set; }
    public bool Attivo { get; set; }
    public bool PasswordCambiata { get; set; }
    public DateTimeOffset CreatoAt { get; set; }
}

public class AdminMedicoUpdateDto
{
    [Required]
    [StringLength(100)]
    public string Nome { get; set; } = null!;

    [Required]
    [StringLength(100)]
    public string Cognome { get; set; } = null!;

    [StringLength(20)]
    public string? Telefono { get; set; }

    [Required]
    [StringLength(150)]
    public string Specializzazione { get; set; } = null!;

    [Required]
    [StringLength(50)]
    public string NumeroIscrizioneAlbo { get; set; } = null!;

    [StringLength(2000)]
    public string? Biografia { get; set; }

    [Range(0.01, 10000)]
    public decimal TariffaVisita { get; set; }

    public bool Attivo { get; set; } = true;
}

public class AdminPazienteDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = null!;
    public string Cognome { get; set; } = null!;
    public string CodiceFiscale { get; set; } = null!;
    public DateOnly DataNascita { get; set; }
    public string? Telefono { get; set; }
    public DateTimeOffset CreatoAt { get; set; }
}

public class AdminPrenotazioneDto
{
    public Guid Id { get; set; }
    public StatoPrenotazione Stato { get; set; }
    public DateTimeOffset DataOraInizio { get; set; }
    public DateTimeOffset DataOraFine { get; set; }
    public Guid MedicoId { get; set; }
    public string MedicoNomeCompleto { get; set; } = null!;
    public string MedicoSpecializzazione { get; set; } = null!;
    public Guid PazienteId { get; set; }
    public string PazienteNomeCompleto { get; set; } = null!;
    public string PazienteCodiceFiscale { get; set; } = null!;
    public decimal? TariffaVisita { get; set; }
    public DateTimeOffset CreatoAt { get; set; }
}

public class SpecializzazioneDto
{
    public Guid Id { get; set; }
    public string Nome { get; set; } = null!;
    public DateTimeOffset CreatoAt { get; set; }
}

public class SpecializzazioneCreateDto
{
    [Required]
    [StringLength(150, MinimumLength = 2)]
    public string Nome { get; set; } = null!;
}

public class AuditLogDto
{
    public Guid Id { get; set; }
    public Guid? AttoreId { get; set; }
    public string? AttoreNome { get; set; }
    public string Azione { get; set; } = null!;
    public string? EntitaTipo { get; set; }
    public Guid? EntitaId { get; set; }
    public string? Dettagli { get; set; }
    public DateTimeOffset CreatoAt { get; set; }
}

public class AdminMedicoCreatoAuditDto
{
    public Guid? MedicoId { get; set; }
    public string? Email { get; set; }
    public string? Nome { get; set; }
    public string? Cognome { get; set; }
}
