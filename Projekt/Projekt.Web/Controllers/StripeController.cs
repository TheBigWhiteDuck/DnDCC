using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Projekt.Services.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace Projekt.Web.Controllers;

/// <summary>
/// Kontroler do obslugi platnosci Stripe Checkout dla subskrypcji Premium.
/// </summary>
[ApiController]
[Route("api/stripe")]
public class StripeController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly IAuthService _authService;
    private readonly ILogger<StripeController> _logger;

    public StripeController(IConfiguration config, IAuthService authService, ILogger<StripeController> logger)
    {
        _config = config;
        _authService = authService;
        _logger = logger;
    }

    [Authorize]
    [HttpPost("create-checkout-session")]
    public async Task<IActionResult> CreateCheckoutSession()
    {
        var secretKey = _config["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            _logger.LogWarning("Stripe secret key missing.");
            return StatusCode(500, new { error = "Stripe configuration missing." });
        }

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { error = "Invalid user context." });
        }

        StripeConfiguration.ApiKey = secretKey;

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var sessionOptions = new SessionCreateOptions
        {
            Mode = "subscription",
            SuccessUrl = $"{baseUrl}/Home/Premium?checkout=success&session_id={{CHECKOUT_SESSION_ID}}",
            CancelUrl = $"{baseUrl}/Home/Premium?checkout=cancel",
            CustomerEmail = User.FindFirstValue(JwtRegisteredClaimNames.Email),
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "pln",
                        UnitAmount = 999,
                        Recurring = new SessionLineItemPriceDataRecurringOptions
                        {
                            Interval = "month"
                        },
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "DnDCC Premium",
                            Description = "Subskrypcja Premium - 9,99 zl / miesiac"
                        }
                    }
                }
            },
            Metadata = new Dictionary<string, string>
            {
                ["userId"] = userId.ToString()
            },
            SubscriptionData = new SessionSubscriptionDataOptions
            {
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId.ToString()
                }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(sessionOptions);

        return Ok(new { url = session.Url });
    }

    [Authorize]
    [HttpGet("confirm-checkout")]
    public async Task<IActionResult> ConfirmCheckout([FromQuery(Name = "session_id")] string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest(new { error = "Missing session_id." });
        }

        var secretKey = _config["Stripe:SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            _logger.LogWarning("Stripe secret key missing.");
            return StatusCode(500, new { error = "Stripe configuration missing." });
        }

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { error = "Invalid user context." });
        }

        StripeConfiguration.ApiKey = secretKey;

        var service = new SessionService();
        var session = await service.GetAsync(sessionId);
        if (session is null)
        {
            return NotFound(new { error = "Session not found." });
        }

        if (!session.Metadata.TryGetValue("userId", out var sessionUserId) || sessionUserId != userId.ToString())
        {
            return Unauthorized(new { error = "Session does not belong to current user." });
        }

        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Payment not completed." });
        }

        var (success, error, result) = await _authService.UpgradeToPremiumAsync(userId);
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
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = result.ExpiresAt,
            }
        );

        return Ok(result);
    }
}
