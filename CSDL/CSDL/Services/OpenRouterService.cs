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
            var lowerPrompt = prompt.ToLower();

            // ❗ Giới hạn chỉ cho phép chủ đề liên quan đến hiến máu hoặc y tế
            string[] allowedKeywords = {
                "hiến máu", "máu", "donate", "blood", "sự kiện", "lịch sử", "đăng ký", "địa điểm", "ngày nào", "huyết học",
                "y tế", "sức khỏe", "bệnh", "triệu chứng", "khám bệnh", "dịch vụ y tế", "bác sĩ", "chăm sóc sức khỏe", "tư vấn y tế"
            };

            if (!allowedKeywords.Any(k => lowerPrompt.Contains(k)))
            {
                return "🤖 Tôi chỉ hỗ trợ các câu hỏi về **hiến máu nhân đạo** và **tư vấn sức khỏe, y tế cơ bản** trên hệ thống này. Mong bạn thông cảm.";
            }

            // ✅ Đưa thông tin sự kiện sắp tới
            var events = GetUpcomingEvents();
            var eventsInfo = events.Any()
                ? string.Join("\n", events.Select(e => $"- {e.EventName} (ngày {e.Date:dd/MM/yyyy} tại {e.Location})"))
                : "Hiện không có sự kiện hiến máu sắp tới.";

            var requestBody = new
            {
                model = _options.Model,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = $@"
🩸 Bạn là trợ lý AI cho hệ thống **Hiến Máu Nhân Đạo HUTECH** và hỗ trợ thông tin Y tế cơ bản.

Trang web này cung cấp:
- Đăng ký tham gia các sự kiện hiến máu sắp tới.
- Tra cứu lịch sử hiến máu của bản thân.
- Cập nhật hồ sơ cá nhân gồm nhóm máu, BHYT và giấy khám sức khỏe.
- Thông tin các sự kiện được tổ chức tại nhiều địa điểm.

Ngoài ra, bạn cũng hỗ trợ các câu hỏi về sức khỏe và tư vấn y tế cơ bản.

📅 Sự kiện sắp diễn ra:
{eventsInfo}

❗ Nếu người dùng hỏi các nội dung ngoài phạm vi hiến máu hoặc Y tế (ví dụ hỏi game, phim, tin tức...), bạn hãy lịch sự từ chối bằng:
“Tôi chỉ hỗ trợ các câu hỏi về **hiến máu nhân đạo** và **tư vấn sức khỏe, y tế cơ bản** trên hệ thống này. Mong bạn thông cảm.”

Hãy trả lời ngắn gọn, rõ ràng, dễ hiểu và chỉ tập trung vào chủ đề hiến máu và y tế."
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
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
