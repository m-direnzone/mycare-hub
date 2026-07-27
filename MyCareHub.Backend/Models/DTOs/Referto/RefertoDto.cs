namespace MyCareHub.Backend.Models.DTOs.Referto;

/// <summary>
/// Documento clinico restituito per l'elenco scaricabile nel Fascicolo Sanitario Elettronico.
/// </summary>
public class RefertoDto
{
    public Guid Id { get; set; }
    public Guid? PrenotazioneId { get; set; }
    public Guid PazienteId { get; set; }
    public Guid MedicoId { get; set; }
    public string Titolo { get; set; } = null!;
    public string? Descrizione { get; set; }
    public DateOnly DataEmissione { get; set; }

    /// <summary>
    /// URL (firmato o endpoint protetto) per lo scaricamento sicuro del PDF.
    /// </summary>
    public string DownloadUrl { get; set; } = null!;

    public string MedicoNomeCompleto { get; set; } = null!;
    public string MedicoSpecializzazione { get; set; } = null!;
}
