using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using MyCareHub.Backend.Controllers;
using MyCareHub.Backend.Entities.Enums;

namespace MyCareHub.Backend.Tests.Permessi;

public class AttributiAutorizzazioneTests
{
    [Fact]
    public void Prenotazioni_paziente_medico_e_admin_hanno_i_ruoli_previsti()
    {
        AssertAuthenticated(typeof(PrenotazioniController));
        AssertRoles(typeof(PrenotazioniController), nameof(PrenotazioniController.PrenotaVisita), "Paziente", "paziente");
        AssertRoles(typeof(PrenotazioniController), nameof(PrenotazioniController.GetMieiAppuntamentiPaziente), "Paziente", "paziente");
        AssertRoles(typeof(PrenotazioniController), nameof(PrenotazioniController.GetAgendaMedico), "Medico", "medico");
        AssertRoles(typeof(PrenotazioniController), nameof(PrenotazioniController.CambiaStato), "Medico", "Paziente", "medico", "paziente");
        AssertRoles(typeof(PrenotazioniController), nameof(PrenotazioniController.ConcludiVisita), "Medico", "medico");
        AssertNoRoleAttribute(typeof(PrenotazioniController), nameof(PrenotazioniController.GetPrenotazioneById));
    }

    [Fact]
    public void Slot_e_referti_separano_paziente_medico_e_admin()
    {
        AssertAuthenticated(typeof(SlotController));
        AssertRoles(typeof(SlotController), nameof(SlotController.GetMiaAgenda), "Medico", "medico");
        AssertRoles(typeof(SlotController), nameof(SlotController.CreazioneInBloccoSlot), "Medico", "Admin", "medico", "admin");
        AssertRoles(typeof(SlotController), nameof(SlotController.EliminaSlot), "Medico", "Admin", "medico", "admin");
        AssertNoRoleAttribute(typeof(SlotController), nameof(SlotController.GetSlotDisponibili));

        AssertAuthenticated(typeof(RefertiController));
        AssertRoles(typeof(RefertiController), nameof(RefertiController.CaricaReferto), "Medico", "medico");
        AssertRoles(typeof(RefertiController), nameof(RefertiController.GetMioFascicoloSanitario), "Paziente", "paziente");
        AssertNoRoleAttribute(typeof(RefertiController), nameof(RefertiController.GetRefertoById));
    }

    [Fact]
    public void Chat_esclude_l_admin_e_l_admin_resta_sulla_propria_policy()
    {
        AssertRoles(typeof(ChatController), null, "Medico", "Paziente", "medico", "paziente");
        var admin = Assert.Single(typeof(AdminController).GetCustomAttributes<AuthorizeAttribute>(inherit: false));
        Assert.Equal(nameof(RuoloUtente.Admin), admin.Policy);
        Assert.True(string.IsNullOrEmpty(admin.Roles));
    }

    private static void AssertAuthenticated(Type controller)
    {
        var authorize = Assert.Single(controller.GetCustomAttributes<AuthorizeAttribute>(inherit: false));
        Assert.True(string.IsNullOrEmpty(authorize.Roles));
        Assert.True(string.IsNullOrEmpty(authorize.Policy));
    }

    private static void AssertRoles(Type controller, string? methodName, params string[] expected)
    {
        var authorize = Assert.Single(Target(controller, methodName).GetCustomAttributes<AuthorizeAttribute>(inherit: false));
        var actual = (authorize.Roles ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Equal(expected.OrderBy(r => r, StringComparer.Ordinal), actual.OrderBy(r => r, StringComparer.Ordinal));
    }

    private static void AssertNoRoleAttribute(Type controller, string methodName)
    {
        Assert.Null(Target(controller, methodName).GetCustomAttribute<AuthorizeAttribute>(inherit: false));
    }

    private static MemberInfo Target(Type controller, string? methodName) =>
        methodName is null
            ? controller
            : controller.GetMethod(methodName) ?? throw new InvalidOperationException(methodName);
}
