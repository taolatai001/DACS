using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CSDL.Services;
using System.Linq;

namespace CSDL.Controllers
{
    [Route("Chat")]
    public class ChatController : Controller
    {
        private readonly OpenRouterService _chatService;

        public ChatController(OpenRouterService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost("Ask")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            string prompt = request.Prompt.ToLower();
            string reply;

            // Các từ khóa liên quan tới sự kiện còn
            string[] eventKeywords = new[]
            {
                "sự kiện còn", "sự kiện sắp", "sự kiện nào còn",
                "sắp diễn ra", "hiến máu ở đâu", "hiến máu ngày nào",
                "có sự kiện nào", "có hiến máu nào", "diễn ra khi nào", "sự kiện hiến máu sắp tới","sự kiện hiến máu"
            };

            // ✅ Các phản hồi thủ công
            if (prompt.Contains("đăng ký hiến máu"))
            {
                reply = "🩸 Để đăng ký hiến máu, bạn hãy đăng nhập vào hệ thống, sau đó vào mục 'Đăng ký Hiến Máu' và chọn sự kiện bạn muốn tham gia.";
            }
            else if (prompt.Contains("quên mật khẩu"))
            {
                reply = "🔐 Nếu bạn quên mật khẩu, hãy vào trang Đăng nhập và chọn 'Quên mật khẩu', hệ thống sẽ gửi lại một mật khẩu mới qua email.";
            }
            else if (prompt.Contains("lịch sử hiến máu"))
            {
                reply = "📋 Sau khi đăng nhập, bạn có thể xem lịch sử hiến máu trong mục 'Lịch sử hiến máu' ở menu người dùng.";
            }
            else if (eventKeywords.Any(k => prompt.Contains(k)))
            {
                var upcomingEvents = _chatService.GetUpcomingEvents();
                if (upcomingEvents.Any())
                {
                    reply = "📅 Các sự kiện sắp tới:\n" +
                        string.Join("\n", upcomingEvents.Select(e => $"- {e.EventName} ({e.Date:dd/MM/yyyy} tại {e.Location})"));
                }
                else
                {
                    reply = "⚠ Hiện không có sự kiện nào sắp diễn ra.";
                }
            }
            else
            {
                // 🧠 Gửi về OpenRouter nếu không khớp từ khóa
                reply = await _chatService.AskAsync(request.Prompt);
            }

            return Json(new { reply });
        }

        public class ChatRequest
        {
            public string Prompt { get; set; }
        }
    }
}
