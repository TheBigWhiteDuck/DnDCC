using System.ComponentModel.DataAnnotations;

namespace Projekt.ViewModel.VM;

/// <summary>
/// Model danych logowania użytkownika, zawiera login/email i hasło.
/// </summary>
public class LoginUserVm
{
    public string UserNameOrEmail { get; set; } = default!;
    public string Password { get; set; } = default!;
}
