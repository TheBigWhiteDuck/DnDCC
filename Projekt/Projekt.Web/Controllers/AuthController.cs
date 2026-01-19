using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Projekt.Services.Interfaces;
using Projekt.ViewModel.VM;

namespace Projekt.Web.Controllers;

/// <summary>
/// Kontroler API obsługujący autoryzację użytkowników: rejestrację, logowanie, wylogowanie, upgrade do premium oraz reset hasła.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    /// <summary>
    /// Tworzy instancję kontrolera autoryzacji.
    /// </summary>
    public AuthController(IAuthService auth) => _auth = auth;

    /// <summary>
    /// Rejestruje nowego użytkownika, przypisuje mu rolę "User" i ustawia token w ciasteczku HttpOnly.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterUserVm vm)
    {
        var (success, error, result) = await _auth.RegisterAsync(vm, "User");
        if (!success)
            return BadRequest(new { error });

        // Set token in HttpOnly cookie
        Response.Cookies.Append(
            "access_token",
            result!.Token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // HTTPS
                SameSite = SameSiteMode.Lax,
                Expires = result.ExpiresAt,
            }
        );

        return Ok(result);
    }

    /// <summary>
    /// Loguje użytkownika po nazwie lub e-mailu i ustawia token JWT w ciasteczku HttpOnly.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginUserVm vm)
    {
        var (success, error, result) = await _auth.LoginAsync(vm);
        if (!success)
            return Unauthorized(new { error });

        Response.Cookies.Append(
            "access_token",
            result!.Token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = result.ExpiresAt,
            }
        );

        return Ok(result);
    }

    /// <summary>
    /// Wylogowuje użytkownika poprzez usunięcie tokena z ciasteczka.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("access_token");
        return NoContent();
    }

    /// <summary>
    /// Aktualizuje konto użytkownika do statusu premium i odświeża token JWT w ciasteczku.
    /// </summary>
    [HttpPost("upgrade-premium")]
    [Authorize]
    public async Task<IActionResult> UpgradePremium()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { error = "Invalid user context." });
        }

        var (success, error, result) = await _auth.UpgradeToPremiumAsync(userId);
        if (!success || result is null)
        {
            return BadRequest(new { error = error ?? "Could not upgrade to premium." });
        }

        Response.Cookies.Append(
            "access_token",
            result.Token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = result.ExpiresAt,
            }
        );

        return Ok(result);
    }

    /// <summary>
    /// Generuje token resetowania hasła i loguje go do konsoli (symulacja wysyłki e-mail).
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        await _auth.SendPasswordResetTokenAsync(vm);
        return Ok(new { message = "If the email exists, reset token has been sent to console." });
    }

    /// <summary>
    /// Resetuje hasło użytkownika na podstawie tokena resetującego.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var (success, error) = await _auth.ResetPasswordAsync(vm);
        if (!success)
            return BadRequest(new { error });
        return NoContent();
    }
}
