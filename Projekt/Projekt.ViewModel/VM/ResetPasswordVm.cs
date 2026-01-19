using System.ComponentModel.DataAnnotations;

namespace Projekt.ViewModel.VM;

/// <summary>
/// Model resetu hasła użytkownika, zawiera login, token i nowe hasło.
/// </summary>
public class ResetPasswordVm
{
    [Required]
    public string UserNameOrEmail { get; set; } = default!;

    [Required]
    public string Token { get; set; } = default!;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = default!;
}