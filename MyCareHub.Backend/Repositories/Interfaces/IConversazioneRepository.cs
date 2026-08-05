using MyCareHub.Backend.Entities;

namespace MyCareHub.Backend.Repositories.Interfaces;

public interface IConversazioneRepository
{
    Task<Conversazione?> GetByIdAsync(Guid id);

    Task<Conversazione?> GetByPartecipantiAsync(Guid pazienteId, Guid medicoId);

    Task<IEnumerable<Conversazione>> GetByPazienteIdAsync(Guid pazienteId);

    Task<IEnumerable<Conversazione>> GetByMedicoIdAsync(Guid medicoId);

    Task<Conversazione?> CreateAsync(Conversazione conversazione);

    Task<bool> TouchUltimoMessaggioAsync(Guid id, DateTimeOffset at);
}
