using Microsoft.AspNetCore.Mvc;
using CSDL.Data;
using CSDL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CSDL.Controllers.Admin
{
    [Route("Notification")]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public NotificationController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ✅ ADMIN: Xem tất cả thông báo
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var notifications = await _context.Notifications
                .Include(n => n.User)  // ✅ Include User để lấy FullName
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];

            return View(notifications);
        }

        // ✅ USER: Xem thông báo của chính mình
        [HttpGet("User")]
        public async Task<IActionResult> UserNotifications()
        {
            var userId = _userManager.GetUserId(User);

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            // Đánh dấu là đã đọc
            foreach (var notification in notifications.Where(n => !n.IsRead))
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();

            ViewBag.UnreadNotificationCount = 0;
            return View("User", notifications);
        }

        // ✅ ADMIN: Gửi thông báo đến tất cả người dùng
        [HttpPost("gui")]
        public async Task<IActionResult> Send(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Nội dung thông báo không được để trống.";
                return RedirectToAction("Index");
            }

            // ✅ Lấy tất cả người dùng trước
            var allUsers = await _userManager.Users.ToListAsync();

            // ✅ Lọc bỏ Admin sau khi đã lấy toàn bộ user từ database
            var targetUsers = new List<User>();

            foreach (var user in allUsers)
            {
                if (!await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    targetUsers.Add(user);
                }
            }

            // ✅ Gửi thông báo cho các user không phải Admin
            foreach (var user in targetUsers)
            {
                var notification = new Notification
                {
                    UserId = user.Id,
                    Title = "Thông báo chung",
                    Message = message,
                    Link = "#",
                    CreatedAt = DateTime.Now
                };
                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã gửi thông báo đến tất cả người dùng.";
            return RedirectToAction("Index");
        }

    }
}
