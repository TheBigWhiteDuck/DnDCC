namespace Projekt.ViewModel.VM;

/// <summary>
/// Model konfiguracji JWT, zawiera dane takie jak Issuer, Audience, SecretKey i czas wygaśnięcia tokenu.
/// </summary>
public class JwtOptionsVm
{
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public string SecretKey { get; set; } = null!;
    public int TokenExpirationMinutes { get; set; }
}
