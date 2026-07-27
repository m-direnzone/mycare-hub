using MyCareHub.Backend.Entities.Enums;

namespace MyCareHub.Backend.Models.DTOs.Profilo;

/// <summary>
/// Payload omnicomprensivo restituito al frontend dopo il login
/// per popolare lo stato dell'applicazione / dashboard utente.
/// </summary>
public class ProfiloDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Email da Supabase Auth (non persistita nella tabella profili).
    /// </summary>
    public string? Email { get; set; }

    public string Nome { get; set; } = null!;
    public string Cognome { get; set; } = null!;
    public string CodiceFiscale { get; set; } = null!;
    public DateOnly DataNascita { get; set; }
    public string? Telefono { get; set; }
    public RuoloUtente Ruolo { get; set; }

    /// <summary>
    /// Popolato solo se l'utente è un medico; evita chiamate aggiuntive dal frontend.
    /// </summary>
    public MedicoDettaglioDto? DettagliMedico { get; set; }
}

/// <summary>
/// Dettagli professionali del medico (estensione anagrafica).
/// </summary>
public class MedicoDettaglioDto
{
    public string Specializzazione { get; set; } = null!;
    public string NumeroIscrizioneAlbo { get; set; } = null!;
    public string? Biografia { get; set; }
    public decimal TariffaVisita { get; set; }
    public bool Attivo { get; set; }
}
