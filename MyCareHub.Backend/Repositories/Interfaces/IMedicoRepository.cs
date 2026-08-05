using MyCareHub.Backend.Entities;

namespace MyCareHub.Backend.Repositories.Interfaces;

public interface IMedicoRepository
{
    Task<bool> CreateMedicoDettagliAsync(MedicoDettaglio dettaglio);
    Task<MedicoDettaglio?> GetDettagliByIdAsync(Guid medicoId);
    Task<bool> UpdateMedicoDettagliAsync(MedicoDettaglio dettaglio);
    Task<bool> ExistsByNumeroAlboAsync(string numeroIscrizioneAlbo);
    Task<bool> MarkPasswordCambiataAsync(Guid medicoId);
    Task<IEnumerable<Profilo>> GetMediciBySpecializzazioneAsync(string specializzazione);
    Task<IEnumerable<(Profilo Profilo, MedicoDettaglio Dettagli)>> GetMediciAttiviAsync(
        string? specializzazione = null,
        string? nome = null);
    Task<IEnumerable<(Profilo Profilo, MedicoDettaglio Dettagli)>> GetTuttiMediciAsync();
    Task<IEnumerable<MedicoDettaglio>> GetAllAttiviAsync();
    Task<IEnumerable<MedicoDettaglio>> GetAllDettagliAsync();
}
