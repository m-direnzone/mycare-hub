using System.Security.Claims;

namespace MyCareHub.Backend.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Estrae l'UUID utente dal claim <c>sub</c> del JWT Supabase.
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirstValue("sub")
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(sub, out var id) ? id : null;
    }

    public static bool IsInAppRole(this ClaimsPrincipal user, string ruolo) =>
        user.IsInRole(ruolo);
}
