using System.IO;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Projekt.DAL;
using Projekt.Model.DataModels;

namespace Projekt.Web.Controllers;

/// <summary>
/// Kontroler API do generowania awatarów postaci graczy przy użyciu OpenAI. Obsługuje generowanie obrazów, zapis i powiązanie z postacią.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AvatarController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    /// <summary>
    /// Tworzy instancję kontrolera awatarów i inicjalizuje zależności: HttpClientFactory, konfigurację, bazę danych i środowisko web.
    /// </summary>
    public AvatarController(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ApplicationDbContext db,
        IWebHostEnvironment env
    )
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _db = db;
        _env = env;
    }

    /// <summary>
    /// Model DTO reprezentujący dane żądania generowania awatara.
    /// </summary>
    public class AvatarRequest
    {
        public int? CharacterId { get; set; }
        public string? PromptOverride { get; set; }
    }

    /// <summary>
    /// Generuje awatar postaci dla użytkowników premium na podstawie promptu lub danych postaci.
    /// </summary>
    [HttpPost("generate")]
    [Authorize]
    public async Task<IActionResult> Generate([FromBody] AvatarRequest req)
    {
        var premiumClaim = User?.FindFirst("premium")?.Value;
        var isPremium = string.Equals(premiumClaim, "true", StringComparison.OrdinalIgnoreCase);
        if (!isPremium)
        {
            return StatusCode(403, "Tylko użytkownicy Premium mogą generować awatary postaci.");
        }

        var apiKey = _config["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
            return BadRequest(new { error = "Brak klucza OpenAI. Ustaw OpenAI:ApiKey." });

        string prompt;
        if (!string.IsNullOrWhiteSpace(req.PromptOverride))
        {
            prompt = req.PromptOverride!;
        }
        else if (req.CharacterId.HasValue)
        {
            var ch = _db.Characters.FirstOrDefault(c => c.Id == req.CharacterId.Value);
            if (ch == null)
                return NotFound(new { error = "Nie znaleziono postaci." });
            prompt = BuildPromptFromCharacter(ch);
        }
        else
        {
            return BadRequest(new { error = "Brak danych do wygenerowania promptu." });
        }

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            apiKey
        );
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json")
        );

        var body = new
        {
            model = "gpt-image-1",
            prompt,
            size = "1024x1024",
        };

        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json"
        );
        var response = await client.PostAsync(
            "https://api.openai.com/v1/images/generations",
            content
        );
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            string? openAiMessage = null;
            try
            {
                using var errDoc = JsonDocument.Parse(err);
                openAiMessage = errDoc
                    .RootElement.GetProperty("error")
                    .GetProperty("message")
                    .GetString();
            }
            catch
            {
                // ignore parse errors
            }
            return StatusCode(
                (int)response.StatusCode,
                new { error = openAiMessage ?? "Błąd OpenAI", detail = err }
            );
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;
        if (
            root.TryGetProperty("data", out var dataEl)
            && dataEl.ValueKind == JsonValueKind.Array
            && dataEl.GetArrayLength() > 0
        )
        {
            var first = dataEl[0];
            if (first.TryGetProperty("url", out var urlEl))
            {
                var url = urlEl.GetString();
                var saved = await SaveImageAsync(url, isDataUrl: false, req.CharacterId);
                return Ok(new { url = saved });
            }
            if (first.TryGetProperty("b64_json", out var b64El))
            {
                var b64 = b64El.GetString();
                var dataUrl = $"data:image/png;base64,{b64}";
                var saved = await SaveImageAsync(dataUrl, isDataUrl: true, req.CharacterId);
                return Ok(new { url = saved });
            }
        }

        return StatusCode(
            500,
            new
            {
                error = "Nie udało się odczytać URL obrazu z odpowiedzi OpenAI.",
                detail = root.ToString(),
            }
        );
    }

    /// <summary>
    /// Zapisuje wygenerowany obraz awatara do katalogu wwwroot/images/avatars i zwraca jego ścieżkę.
    /// </summary>
    private async Task<string> SaveImageAsync(string source, bool isDataUrl, int? characterId)
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var avatarDir = Path.Combine(webRoot, "images", "avatars");
        Directory.CreateDirectory(avatarDir);

        var fileName = $"avatar_{characterId ?? 0}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.png";
        var filePath = Path.Combine(avatarDir, fileName);
        var stableName =
            characterId.HasValue && characterId.Value > 0
                ? $"avatar_{characterId.Value}.png"
                : $"avatar_latest.png";
        var stablePath = Path.Combine(avatarDir, stableName);

        byte[] bytes;
        if (isDataUrl && source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = source.IndexOf(',');
            var b64 = comma >= 0 ? source[(comma + 1)..] : source;
            bytes = Convert.FromBase64String(b64);
        }
        else
        {
            bytes = await _httpClientFactory.CreateClient().GetByteArrayAsync(source);
        }

        await System.IO.File.WriteAllBytesAsync(filePath, bytes);
        await System.IO.File.WriteAllBytesAsync(stablePath, bytes);
        return $"/images/avatars/{stableName}";
    }

    /// <summary>
    /// Buduje prompt tekstowy dla OpenAI na podstawie właściwości postaci (rasa, klasa, alignment).
    /// </summary>
    private static string BuildPromptFromCharacter(Character ch)
    {
        var sb = new StringBuilder();
        sb.Append($"Fantasy portrait of a {ch.Race} {ch.Class}");
        if (!string.IsNullOrWhiteSpace(ch.Alignment))
            sb.Append($", alignment {ch.Alignment}");
        sb.Append(
            ", detailed, dramatic lighting, digital painting, head and shoulders, background subtle"
        );
        return sb.ToString();
    }
}
