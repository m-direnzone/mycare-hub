using MyCareHub.Backend.Models.DTOs.Auth;

namespace MyCareHub.Backend.Services.Interfaces;

public interface ISupabaseAuthAdminService
{
    /// <summary>
    /// Crea un utente Auth con ruolo medico, password temporanea e flag must_change_password.
    /// </summary>
    Task<MedicoAuthCreateResult> CreateMedicoUserAsync(
        MedicoOnboardingDto dto,
        string passwordTemporanea,
        CancellationToken cancellationToken = default);

    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class MedicoAuthCreateResult
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
}
