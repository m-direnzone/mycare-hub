using MyCareHub.Backend.Models.DTOs.Admin;

namespace MyCareHub.Backend.Services.Interfaces;

public interface IAdminService
{
    Task<AdminKpiDto> GetKpiAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<AdminMedicoDto>> GetMediciAsync(CancellationToken cancellationToken = default);

    Task<bool> AggiornaMedicoAsync(
        Guid medicoId,
        AdminMedicoUpdateDto dto,
        Guid attoreId,
        string attoreNome);

    Task<bool> SoftDeleteMedicoAsync(Guid medicoId, Guid attoreId, string attoreNome);

    Task<bool> RiattivaMedicoAsync(Guid medicoId, Guid attoreId, string attoreNome);

    Task<IEnumerable<AdminPazienteDto>> GetPazientiAsync();

    Task<IEnumerable<AdminPrenotazioneDto>> GetPrenotazioniAsync(
        int? anno = null,
        int? mese = null,
        int? giorno = null);

    Task<IEnumerable<AuditLogDto>> GetAuditLogAsync(int limit = 200);

    Task LogAsync(
        Guid? attoreId,
        string? attoreNome,
        string azione,
        string? entitaTipo = null,
        Guid? entitaId = null,
        string? dettagli = null);

    Task<IEnumerable<SpecializzazioneDto>> GetSpecializzazioniAsync();

    Task<SpecializzazioneDto> CreaSpecializzazioneAsync(
        SpecializzazioneCreateDto dto,
        Guid attoreId,
        string attoreNome);

    Task<bool> EliminaSpecializzazioneAsync(Guid id, Guid attoreId, string attoreNome);
}
