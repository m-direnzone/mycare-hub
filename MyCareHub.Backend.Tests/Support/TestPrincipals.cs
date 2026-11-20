using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyCareHub.Backend.Entities.Enums;

namespace MyCareHub.Backend.Tests.Support;

internal static class TestPrincipals
{
    public static ClaimsPrincipal Paziente(Guid id) => Create(id, nameof(RuoloUtente.Paziente), "paziente");

    public static ClaimsPrincipal Medico(Guid id) => Create(id, nameof(RuoloUtente.Medico), "medico");

    public static ClaimsPrincipal Admin(Guid id) => Create(id, nameof(RuoloUtente.Admin), "admin");

    public static ClaimsPrincipal Anonimo() => new(new ClaimsIdentity());

    public static void Attach(ControllerBase controller, ClaimsPrincipal user)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    public static IEnumerable<T> Sequence<T>(ActionResult<IEnumerable<T>> action)
    {
        var value = action.Result is ObjectResult objectResult ? objectResult.Value : action.Value;
        return Assert.IsAssignableFrom<IEnumerable<T>>(value);
    }

    private static ClaimsPrincipal Create(Guid id, params string[] ruoli)
    {
        var claims = new List<Claim> { new("sub", id.ToString()) };
        claims.AddRange(ruoli.Select(ruolo => new Claim(ClaimTypes.Role, ruolo)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "test"));
    }
}
