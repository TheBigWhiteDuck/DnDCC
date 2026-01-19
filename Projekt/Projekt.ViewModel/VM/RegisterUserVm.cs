namespace Projekt.ViewModel.VM;

/// <summary>
/// Model danych rejestracji użytkownika, zawiera login, email, hasło, imię, nazwisko i status Premium.
/// </summary>
public class RegisterUserVm
{
    public string UserName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsPremium { get; set; } = false;
}
