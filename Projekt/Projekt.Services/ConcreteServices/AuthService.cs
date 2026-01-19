using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Projekt.Model.DataModels;
using Projekt.Services.Interfaces;
using Projekt.ViewModel.VM;

namespace Projekt.Services.ConcreteServices;

/// <summary>
/// Serwis odpowiedzialny za autoryzację użytkowników: rejestrację, logowanie, reset hasła, generowanie tokenów JWT oraz upgrade do premium.
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthService> _logger;

    /// <summary>
    /// Tworzy instancję serwisu autoryzacji.
    /// </summary>
    public AuthService(
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IConfiguration config,
        ILogger<AuthService> logger
    )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Rejestruje nowego użytkownika i przypisuje mu rolę.
    /// </summary>
    public async Task<(bool Success, string? Error, AuthResultVm? Result)> RegisterAsync(
        RegisterUserVm input,
        string role = "User"
    )
    {
        var existing =
            await _userManager.FindByNameAsync(input.UserName)
            ?? await _userManager.FindByEmailAsync(input.Email);
        if (existing != null)
            return (false, "User already exists", null);

        var user = new User
        {
            UserName = input.UserName,
            Email = input.Email,
            EmailConfirmed = true,
            FirstName = input.FirstName,
            LastName = input.LastName,
            IsPremium = input.IsPremium,
        };
        var create = await _userManager.CreateAsync(user, input.Password);
        if (!create.Succeeded)
            return (false, string.Join("; ", create.Errors.Select(e => e.Description)), null);

        if (!await _roleManager.RoleExistsAsync(role))
            await _roleManager.CreateAsync(new Role { Name = role });

        await _userManager.AddToRoleAsync(user, role);

        var result = await GenerateTokenAsync(user);
        return (true, null, result);
    }

    /// <summary>
    /// Loguje użytkownika na podstawie nazwy użytkownika lub e-maila i hasła.
    /// </summary>
    public async Task<(bool Success, string? Error, AuthResultVm? Result)> LoginAsync(
        LoginUserVm input
    )
    {
        var user =
            await _userManager.FindByNameAsync(input.UserNameOrEmail)
            ?? await _userManager.FindByEmailAsync(input.UserNameOrEmail);
        if (user == null)
            return (false, "Invalid credentials", null);

        var valid = await _userManager.CheckPasswordAsync(user, input.Password);
        if (!valid)
            return (false, "Invalid credentials", null);

        var result = await GenerateTokenAsync(user);
        return (true, null, result);
    }

    /// <summary>
    /// Generuje token JWT dla użytkownika wraz z rolami i dodatkowymi danymi.
    /// </summary>
    private async Task<AuthResultVm> GenerateTokenAsync(User user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var jwtSection = _config.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        };

        if (!string.IsNullOrWhiteSpace(user.FirstName))
            claims.Add(new Claim(ClaimTypes.GivenName, user.FirstName));
        if (!string.IsNullOrWhiteSpace(user.LastName))
            claims.Add(new Claim(ClaimTypes.Surname, user.LastName));
        claims.Add(new Claim("premium", user.IsPremium ? "true" : "false"));

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSection["ExpiryMinutes"]!));
        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        return new AuthResultVm
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expires,
            UserName = user.UserName ?? string.Empty,
            Roles = roles,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsPremium = user.IsPremium,
        };
    }

    /// <summary>
    /// Generuje token resetowania hasła i loguje go (symulacja wysyłki e-mail).
    /// </summary>
    public async Task<(bool success, string? error)> SendPasswordResetTokenAsync(
        ForgotPasswordVm vm
    )
    {
        var user = await _userManager.FindByEmailAsync(vm.Email);
        if (user is null)
        {
            _logger.LogInformation("Password reset requested (email not found): {email}", vm.Email);
            return (true, null);
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        var urlEncoded = HttpUtility.UrlEncode(token);

        Console.WriteLine("[Password Reset]");
        Console.WriteLine($"User: {user.UserName}");
        Console.WriteLine($"Token: {token}");

        _logger.LogInformation("Password reset token generated for {user}", user.UserName);
        return (true, null);
    }

    /// <summary>
    /// Resetuje hasło użytkownika na podstawie tokena resetującego.
    /// </summary>
    public async Task<(bool success, string? error)> ResetPasswordAsync(ResetPasswordVm vm)
    {
        var user = vm.UserNameOrEmail.Contains('@')
            ? await _userManager.FindByEmailAsync(vm.UserNameOrEmail)
            : await _userManager.FindByNameAsync(vm.UserNameOrEmail);

        if (user is null)
            return (true, null);

        var result = await _userManager.ResetPasswordAsync(user, vm.Token, vm.NewPassword);
        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(e => e.Description));
            _logger.LogWarning(
                "Password reset failed for {user}: {error}",
                vm.UserNameOrEmail,
                error
            );
            return (false, error);
        }
        _logger.LogInformation("Password reset succeeded for {user}", vm.UserNameOrEmail);
        return (true, null);
    }

    /// <summary>
    /// Uaktualnia konto użytkownika do statusu premium i generuje nowy token.
    /// </summary>
    public async Task<(bool Success, string? Error, AuthResultVm? Result)> UpgradeToPremiumAsync(
        int userId
    )
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return (false, "User not found", null);
        }

        if (!user.IsPremium)
        {
            user.IsPremium = true;
            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
            {
                var error = string.Join("; ", update.Errors.Select(e => e.Description));
                _logger.LogWarning("Premium upgrade failed for {userId}: {error}", userId, error);
                return (false, error, null);
            }
            _logger.LogInformation("Premium upgraded for user {userId}", userId);
        }

        var result = await GenerateTokenAsync(user);
        return (true, null, result);
    }

    /// <summary>
    /// Wyszukuje użytkownika po nazwie lub e-mailu.
    /// </summary>
    private async Task<User?> FindByUserNameOrEmailAsync(string userNameOrEmail)
    {
        User? user = null;
        if (userNameOrEmail.Contains('@'))
            user = await _userManager.FindByEmailAsync(userNameOrEmail);
        if (user is null)
            user = await _userManager.FindByNameAsync(userNameOrEmail);
        return user;
    }
}
