using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using CSDL.Data;
using CSDL.Models;
using CSDL.Services;

namespace CSDL.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class BloodDonationAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public BloodDonationAdminController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ✅ Hiển thị tất cả đăng ký
        public async Task<IActionResult> Index(string search)
        {
            var donationsQuery = _context.BloodDonations
                .Include(d => d.User)
                .Include(d => d.Event);

            var donations = await donationsQuery.ToListAsync(); // Truy vấn DB trước

            if (!string.IsNullOrEmpty(search))
            {
                // Lọc trên bộ nhớ vì dùng ToString()
                donations = donations
                    .Where(d =>
                        (!string.IsNullOrEmpty(d.User.FullName) && d.User.FullName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(d.User.PhoneNumber) && d.User.PhoneNumber.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(d.BloodType) && d.BloodType.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrEmpty(d.Event.EventName) && d.Event.EventName.Contains(search, StringComparison.OrdinalIgnoreCase)) ||
                        d.RegistrationDate.ToString("yyyy-MM-dd").Contains(search) ||
                        d.Status.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();
            }

            return View(donations);
        }


        // ✅ Admin xác nhận người hiến máu
        [HttpPost]
        public async Task<IActionResult> Confirm(int id)
        {
            var donation = await _context.BloodDonations
                .Include(d => d.User)
                .Include(d => d.Event)
                .FirstOrDefaultAsync(d => d.DonationID == id);

            if (donation == null || donation.User == null || donation.Event == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đăng ký.";
                return RedirectToAction(nameof(Index));
            }

            if (donation.Status == BloodDonationStatus.Completed)
            {
                TempData["WarningMessage"] = "Đơn này đã được xác nhận trước đó.";
                return RedirectToAction(nameof(Index));
            }

            donation.Status = BloodDonationStatus.Completed;
            await _context.SaveChangesAsync();

            // Gửi mail xác nhận
            string subject = "✅ Xác nhận hiến máu thành công!";
            string body = $@"
                Xin chào {donation.User.FullName},<br/>
                Đơn đăng ký hiến máu của bạn tại sự kiện <strong>{donation.Event.EventName}</strong> 
                ngày <strong>{donation.Event.Date:dd/MM/yyyy}</strong> đã được xác nhận thành công.<br/><br/>
                Cảm ơn bạn đã lan tỏa yêu thương ❤️.
            ";

            await _emailService.SendEmailAsync(donation.User.Email, subject, body);

            TempData["SuccessMessage"] = "✅ Đã xác nhận hiến máu!";
            return RedirectToAction(nameof(Index));
        }

        // ✅ Xóa đơn đăng ký
        [HttpPost]
        [ValidateAntiForgeryToken] // ✅ Tăng bảo mật nếu dùng form
        public async Task<IActionResult> Delete(int id)
        {
            var donation = await _context.BloodDonations
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DonationID == id);

            if (donation == null)
            {
                TempData["ErrorMessage"] = "❌ Không tìm thấy đơn đăng ký để xóa.";
                return RedirectToAction(nameof(Index));
            }

            _context.BloodDonations.Remove(donation);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"🗑️ Đã xóa đơn đăng ký của <strong>{donation.User?.FullName ?? "người dùng"}</strong>!";
            return RedirectToAction(nameof(Index));
        }

    }
}
