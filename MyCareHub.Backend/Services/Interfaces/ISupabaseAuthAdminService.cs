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

    /// <summary>
    /// Recupera email e flag must_change_password per un set di utenti Auth.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, AuthUserAdminInfo>> GetUsersAdminInfoAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);
}

public sealed class MedicoAuthCreateResult
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
}

public sealed class AuthUserAdminInfo
{
    public required Guid UserId { get; init; }
    public string? Email { get; init; }
    public bool MustChangePassword { get; init; }
}
