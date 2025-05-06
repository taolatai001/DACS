using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using CSDL.Configurations;
using CSDL.Models;
using Microsoft.EntityFrameworkCore;
using CSDL.Data;

namespace CSDL.Services
{
    public class OpenRouterService
    {
        private readonly HttpClient _httpClient;
        private readonly OpenRouterOptions _options;
        private readonly ApplicationDbContext _context;

        public OpenRouterService(HttpClient httpClient, IOptions<OpenRouterOptions> options, ApplicationDbContext context)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _context = context;
        }

        public List<BloodDonationEvent> GetUpcomingEvents()
        {
            return _context.BloodDonationEvents
                           .Where(e => e.Date >= DateTime.Today)
                           .OrderBy(e => e.Date)
                           .Take(5)
                           .ToList();
        }

        public async Task<string> AskAsync(string prompt)
        {
            var requestBody = new
            {
                model = _options.Model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

            var response = await _httpClient.PostAsync(_options.BaseUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return $"❌ Lỗi: {response.StatusCode}";

            using var doc = JsonDocument.Parse(responseContent);
            return doc.RootElement
                      .GetProperty("choices")[0]
                      .GetProperty("message")
                      .GetProperty("content")
                      .GetString()
                      ?.Trim() ?? "Không có phản hồi từ chatbot.";
        }
    }
}
