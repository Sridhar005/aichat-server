using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace AIChatApp.Services;

public class GeminiClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    private const string BaseUrl =
        "https://generativelanguage.googleapis.com/v1beta/models/gemini-3-flash-preview:generateContent";

    public GeminiClient(HttpClient httpClient, IConfiguration config)
    {
        _http = httpClient;

        _apiKey = config["Gemini:ApiKey"]
            ?? throw new Exception("Gemini API key missing");
    }

    public async Task<string> GetReplyAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.7,
                maxOutputTokens = 500
            }
        };

        var json = JsonSerializer.Serialize(body);

        var request = new HttpRequestMessage(HttpMethod.Post,
            $"{BaseUrl}?key={_apiKey}")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        // Explicitly accept JSON and give servers a hint about the request content
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _http.SendAsync(request, cancellationToken);
        var result = await response.Content.ReadAsStringAsync();

        // Surface useful error information to help debugging API key / request issues
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Gemini API failed: {(int)response.StatusCode} {response.ReasonPhrase} - {result}");
        }
        using var doc = JsonDocument.Parse(result);

        if (!doc.RootElement.TryGetProperty("candidates", out var candidates)
            || candidates.ValueKind != JsonValueKind.Array
            || candidates.GetArrayLength() == 0)
        {
            throw new Exception($"Gemini returned no candidates - raw response: {result}");
        }

        var first = candidates[0];

        // Safely navigate nested properties
        if (!first.TryGetProperty("content", out var content)
            || !content.TryGetProperty("parts", out var parts)
            || parts.ValueKind != JsonValueKind.Array
            || parts.GetArrayLength() == 0)
        {
            throw new Exception($"Unexpected Gemini response shape - raw response: {result}");
        }

        var part = parts[0];

        if (!part.TryGetProperty("text", out var textElement))
        {
            throw new Exception($"Gemini returned no text part - raw response: {result}");
        }

        var text = textElement.GetString();

        if (string.IsNullOrEmpty(text))
        {
            throw new Exception($"Gemini returned empty response - raw response: {result}");
        }

        return text;
    }
}