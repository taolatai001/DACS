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
            string prompt = request.Prompt?.ToLower()?.Trim();
            string reply;

            if (string.IsNullOrEmpty(prompt))
                return Json(new { reply = "❗ Vui lòng nhập câu hỏi." });

            // ✅ Các từ khóa thủ công
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
            else
            {
                // ✅ Dữ liệu sẽ được xử lý AI, nhưng đã được giới hạn trong OpenRouterService
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
