using CSDL.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Options;

public class OpenAIService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAIOptions _options;

    public OpenAIService(HttpClient httpClient, IOptions<OpenAIOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> AskChatGPT(string prompt)
    {
        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.7
        };

        var requestJson = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", _options.Referer);
        _httpClient.DefaultRequestHeaders.Add("X-Title", _options.Title);

        var response = await _httpClient.PostAsync(_options.BaseUrl, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseJson);
        var result = doc.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("message")
                        .GetProperty("content")
                        .GetString();

        return result?.Trim();
    }
}
