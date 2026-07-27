using MyCareHub.Backend.Entities;

namespace MyCareHub.Backend.Repositories.Interfaces;

public interface IMedicoRepository
{
    Task<bool> CreateMedicoDettagliAsync(MedicoDettaglio dettaglio);
    Task<MedicoDettaglio?> GetDettagliByIdAsync(Guid medicoId);
    Task<bool> UpdateMedicoDettagliAsync(MedicoDettaglio dettaglio);
    Task<IEnumerable<Profilo>> GetMediciBySpecializzazioneAsync(string specializzazione);
    Task<IEnumerable<MedicoDettaglio>> GetAllAttiviAsync();
}
