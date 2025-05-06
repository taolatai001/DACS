using Microsoft.AspNetCore.Mvc;
using CSDL.Data;
using CSDL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CSDL.Controllers.Admin
{
    [Route("admin/thong-bao")]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: admin/thong-bao
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var notifications = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
            ViewBag.Success = TempData["Success"];
            ViewBag.Error = TempData["Error"];
            return View(notifications);
        }
        [HttpGet("/Notification/User")]
        public async Task<IActionResult> User()
        {
            var notifications = await _context.Notifications
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View("User", notifications);
        }
        // POST: admin/thong-bao/gui
        [HttpPost("gui")]
        public async Task<IActionResult> Send(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                TempData["Error"] = "Nội dung thông báo không được để trống.";
                return RedirectToAction("Index");
            }

            var notification = new Notification
            {
                Message = message,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Gửi thông báo thành công.";
            return RedirectToAction("Index");
        }
    }
}
