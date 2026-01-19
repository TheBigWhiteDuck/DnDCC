namespace Projekt.ViewModel.VM;

/// <summary>
/// Model reprezentujący wynik uwierzytelnienia użytkownika, w tym token, datę wygaśnięcia i role.
/// </summary>
public class AuthResultVm
{
    public string Token { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public string UserName { get; set; } = default!;
    public IList<string> Roles { get; set; } = new List<string>();
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsPremium { get; set; }
}