using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Projekt.Model.DataModels;

/// <summary>
/// Reprezentuje użytkownika aplikacji.
/// Dziedziczy po IdentityUser i rozszerza go o dane domenowe.
/// </summary>
public class User : IdentityUser<int>
{
    /// <summary>
    /// Imię użytkownika.
    /// </summary>
    [MaxLength(100)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Nazwisko użytkownika.
    /// </summary>
    [MaxLength(100)]
    public string? LastName { get; set; }

    /// <summary>
    /// Określa, czy użytkownik posiada konto premium.
    /// </summary>
    public bool IsPremium { get; set; }

    /// <summary>
    /// Lista postaci przypisanych do użytkownika.
    /// </summary>
    public virtual IList<Character> Characters { get; set; } = new List<Character>();
}
