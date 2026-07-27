using System.ComponentModel.DataAnnotations;

namespace MyCareHub.Backend.Models.DTOs.Auth;

/// <summary>
/// Credenziali per l'autenticazione (Supabase Auth).
/// </summary>
public class LoginDto
{
    [Required(ErrorMessage = "L'email è obbligatoria.")]
    [EmailAddress(ErrorMessage = "Formato email non valido.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "La password è obbligatoria.")]
    public string Password { get; set; } = null!;
}
