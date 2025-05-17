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
        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var donation = await _context.BloodDonations
                .Include(d => d.User)
                .Include(d => d.Event)
                .FirstOrDefaultAsync(d => d.DonationID == id);

            if (donation == null || donation.User == null || donation.Event == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đăng ký.";
                return RedirectToAction("Index");
            }

            if (donation.Status != BloodDonationStatus.Pending)
            {
                TempData["WarningMessage"] = "Chỉ có thể từ chối đơn đang chờ xác nhận.";
                return RedirectToAction("Index");
            }

            // ✅ Gán đúng trạng thái TỪ CHỐI
            donation.Status = BloodDonationStatus.Rejected;
            await _context.SaveChangesAsync();

            // ✅ Nội dung email thông báo
            string subject = "❌ Đăng ký hiến máu bị từ chối";
            string body = $@"
        Xin chào {donation.User.FullName},<br/>
        Đơn đăng ký hiến máu của bạn tại sự kiện <strong>{donation.Event.EventName}</strong> 
        ngày <strong>{donation.Event.Date:dd/MM/yyyy}</strong> đã bị <strong>từ chối</strong> vì lý do sức khỏe hoặc thông tin chưa đầy đủ.<br/><br/>
        Vui lòng liên hệ với ban tổ chức để biết thêm chi tiết.";

            await _emailService.SendEmailAsync(donation.User.Email, subject, body);

            TempData["SuccessMessage"] = "❌ Đã từ chối đăng ký hiến máu.";
            return RedirectToAction("Index");
        }


        [HttpGet]
        public async Task<IActionResult> GenerateCertificate(int donationId)
        {
            var donation = await _context.BloodDonations
                .Include(d => d.User)
                .Include(d => d.Event)
                .FirstOrDefaultAsync(d => d.DonationID == donationId && d.Status == BloodDonationStatus.Completed);

            if (donation == null)
                return Json(new { success = false, message = "Không tìm thấy đăng ký hoặc chưa được xác nhận." });

            if (donation.IsCertificateIssued)
                return Json(new { success = false, message = "Giấy chứng nhận đã được cấp trước đó." });

            // Đánh dấu đã cấp
            donation.IsCertificateIssued = true;
            await _context.SaveChangesAsync();
            // ✅ Tạo nội dung HTML đầy đủ
            var htmlContent = $@"
    <html>
    <head>
        <meta charset='utf-8'>
        <title>Giấy Chứng Nhận</title>
    </head>
    <body style='text-align:center; font-family: Arial, sans-serif; padding: 50px; border: 5px solid red;'>
        <h1 style='color: darkred;'>GIẤY CHỨNG NHẬN HIẾN MÁU NHÂN ĐẠO</h1>
        <p>Chứng nhận rằng <strong>{donation.User.FullName}</strong></p>
        <p>Đã hiến máu tại sự kiện <strong>{donation.Event.EventName}</strong></p>
        <p>Địa điểm: <strong>{donation.Event.Location}</strong></p>
        <p>Ngày: <strong>{donation.Event.Date:dd/MM/yyyy}</strong></p>
        <p>Nhóm máu: <strong>{donation.User.BloodType}</strong></p>
        <div style='margin-top:50px; font-style: italic;'>
            <p>Xin chân thành cảm ơn nghĩa cử cao đẹp của bạn.</p>
            <p>Ngày cấp: {DateTime.Now:dd/MM/yyyy}</p>
        </div>
    </body>
    </html>";

            // Tạo file tạm

            var htmlFilePath = Path.Combine(Path.GetTempPath(), $"certificate_{donationId}.html");
            await System.IO.File.WriteAllTextAsync(htmlFilePath, htmlContent);

            var wkhtmlPath = @"C:\Program Files\wkhtmltopdf\bin\wkhtmltopdf.exe";
            var outputPdfPath = Path.Combine(Path.GetTempPath(), $"certificate_{donationId}.pdf");

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = wkhtmlPath,
                Arguments = $"\"{htmlFilePath}\" \"{outputPdfPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            await process.StandardOutput.ReadToEndAsync();
            await process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            if (!System.IO.File.Exists(outputPdfPath))
            {
                return Json(new { success = false, message = "Lỗi tạo giấy chứng nhận." });
            }

            // Tạo thông báo
            var notification = new Notification
            {
                UserId = donation.UserID,
                Title = "Giấy chứng nhận hiến máu đã sẵn sàng",
                Message = $"Bạn đã hiến máu thành công tại sự kiện {donation.Event.EventName}. Giấy chứng nhận đã được cấp.",
                Link = Url.Action("DownloadCertificate", "BloodDonationAdmin", new { donationId }, Request.Scheme),
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var downloadLink = Url.Action("DownloadCertificate", "BloodDonationAdmin", new { donationId }, Request.Scheme);
            return Json(new { success = true, message = "Cấp giấy chứng nhận thành công.", link = downloadLink });
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
        // ✅ Xem chi tiết đơn đăng ký hiến máu
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var donation = await _context.BloodDonations
                .Include(d => d.User)
                .Include(d => d.Event)
                .FirstOrDefaultAsync(d => d.DonationID == id);

            if (donation == null) return NotFound();

            return View("Details", donation);
        }

    }
}
