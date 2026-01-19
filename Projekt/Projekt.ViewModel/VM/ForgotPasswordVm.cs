using System.ComponentModel.DataAnnotations;

namespace Projekt.ViewModel.VM;

/// <summary>
/// Model danych do formularza przypomnienia hasła.
/// </summary>
public class ForgotPasswordVm
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = default!;
}
