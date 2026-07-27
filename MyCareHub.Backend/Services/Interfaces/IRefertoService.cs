using MyCareHub.Backend.Models.DTOs.Referto;

namespace MyCareHub.Backend.Services.Interfaces;

public interface IRefertoService
{
    /// <summary>
    /// Registra i metadati di un referto associandoli al path del file su Storage.
    /// </summary>
    Task<RefertoDto?> SalvaMetaRefertoAsync(Guid medicoId, RefertoCreateDto dto, string storageFilePath);

    /// <summary>
    /// Restituisce il fascicolo sanitario elettronico (tutti i referti) del paziente,
    /// con Signed URL a tempo per il download GDPR-compliant.
    /// </summary>
    Task<IEnumerable<RefertoDto>> GetFascicoloSanitarioPazienteAsync(Guid pazienteId);

    /// <summary>
    /// Recupera un singolo referto con Signed URL a tempo.
    /// </summary>
    Task<RefertoDto?> GetRefertoByIdAsync(Guid refertoId);
}
