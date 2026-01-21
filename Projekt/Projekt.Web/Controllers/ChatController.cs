using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Projekt.Web.Controllers;

/// <summary>
/// Kontroler API do rozmowy z asystentem DnDCC AI.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public ChatController(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class ChatRequest
    {
        public List<ChatMessage> Messages { get; set; } = new();
    }

    [HttpPost("dnd")]
    [Authorize]
    public async Task<IActionResult> DndChat([FromBody] ChatRequest request)
    {
        var apiKey = _config["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return BadRequest(new { error = "Brak klucza OpenAI. Ustaw OpenAI:ApiKey." });
        }

        var systemPrompt =
            "Jestes pomocnikiem DnDCC AI. Odpowiadaj tylko na pytania zwiazane z gra Dungeons & Dragons (mechanika, lore, zasady, sesje, postacie, przedmioty). "
            + "Jesli pytanie nie dotyczy DnD, odpowiedz: 'Moge odpowiadac tylko na pytania dotyczace Dungeons & Dragons.' "
            + "Odpowiadaj krotko i jasno w jezyku polskim.";

        var safeMessages = (request.Messages ?? new List<ChatMessage>())
            .Where(m => m != null
                        && !string.IsNullOrWhiteSpace(m.Content)
                        && (string.Equals(m.Role, "user", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(m.Role, "assistant", StringComparison.OrdinalIgnoreCase)))
            .Select(m => new
            {
                role = string.Equals(m.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                    ? "assistant"
                    : "user",
                content = m.Content.Trim(),
            })
            .ToList();

        if (safeMessages.Count > 8)
        {
            safeMessages = safeMessages.Skip(safeMessages.Count - 8).ToList();
        }

        var messages = new List<object>
        {
            new { role = "system", content = systemPrompt },
        };
        messages.AddRange(safeMessages);

        var body = new
        {
            model = "gpt-4o-mini",
            messages,
            temperature = 0.7,
            max_tokens = 400,
        };

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);
        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, new { error = "Blad OpenAI", detail = err });
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        using var doc = await JsonDocument.ParseAsync(stream);
        var root = doc.RootElement;
        var reply = root
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return Ok(new { reply = reply ?? string.Empty });
    }
}
