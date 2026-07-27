using MyCareHub.Backend.Entities;

namespace MyCareHub.Backend.Repositories.Interfaces;

public interface IRefertoMedicoRepository
{
    Task<RefertoMedico?> GetByIdAsync(Guid id);

    /// <summary>
    /// Referti clinici di un paziente.
    /// </summary>
    Task<IEnumerable<RefertoMedico>> GetByPazienteIdAsync(Guid pazienteId);

    /// <summary>
    /// Referti emessi da un medico.
    /// </summary>
    Task<IEnumerable<RefertoMedico>> GetByMedicoIdAsync(Guid medicoId);

    /// <summary>
    /// Salva la riga di tracciamento del referto (file_path verso Supabase Storage).
    /// </summary>
    Task<RefertoMedico?> CreateAsync(RefertoMedico referto);

    /// <summary>
    /// Elimina il record del referto (la cancellazione del file sul bucket resta a un servizio dedicato).
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
}
