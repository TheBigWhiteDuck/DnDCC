using Projekt.ViewModel.VM;

namespace Projekt.Services.Interfaces;

public interface IAuthService
{
    Task<(bool Success, string? Error, AuthResultVm? Result)> RegisterAsync(RegisterUserVm input, string role = "User");
    Task<(bool Success, string? Error, AuthResultVm? Result)> LoginAsync(LoginUserVm input);
    Task<(bool success, string? error)> SendPasswordResetTokenAsync(ForgotPasswordVm vm);
    Task<(bool success, string? error)> ResetPasswordAsync(ResetPasswordVm vm);
    Task<(bool Success, string? Error, AuthResultVm? Result)> UpgradeToPremiumAsync(int userId);
}