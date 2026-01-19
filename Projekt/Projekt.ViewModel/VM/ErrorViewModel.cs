namespace Projekt.ViewModel.VM;

/// <summary>
/// Model danych dla strony błędu, przechowuje RequestId i informację czy wyświetlić identyfikator.
/// </summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
