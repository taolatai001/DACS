using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using CSDL.Data;
using CSDL.Models;
using CSDL.ViewModels;
using CSDL.Services;
using DocumentFormat.OpenXml.EMMA; // ✅ Import EmailService nếu cần gửi email

namespace CSDL.Controllers
{
    [Authorize]
    public class BloodDonationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly EmailService _emailService; // ✅ Thêm EmailService để gửi email (tùy chọn)

        public BloodDonationController(ApplicationDbContext context, UserManager<User> userManager, EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService; // ✅ Khởi tạo EmailService
        }

        // ✅ Hiển thị danh sách sự kiện hiến máu
        public async Task<IActionResult> Index(string search)
        {
            var events = await _context.BloodDonationEvents
                .Where(b => b.Date >= DateTime.Today) // Chỉ lấy các sự kiện chưa diễn ra
                .ToListAsync(); // Chuyển sang List để xử lý LINQ trên bộ nhớ

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();

                events = events.Where(b =>
                    (!string.IsNullOrEmpty(b.EventName) && b.EventName.ToLower().Contains(search)) ||
                    (!string.IsNullOrEmpty(b.Location) && b.Location.ToLower().Contains(search)) ||
                    b.Date.ToString("yyyy-MM-dd").Contains(search)
                ).ToList();
            }

            return View(events);
        }



        // ✅ Hiển thị form nhập thông tin đăng ký
        [HttpGet]
        public async Task<IActionResult> RegisterForm(int eventId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var model = new BloodDonationRegisterViewModel
            {
                EventID = eventId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                BloodType = user.BloodType,
                IsBloodTypeLocked = user.IsBloodTypeLocked // ✅ Thêm dòng này
            };

            return View(model);
        }


        // ✅ Hiển thị lịch sử hiến máu của User
        public async Task<IActionResult> History()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var donations = await _context.BloodDonations
                .Include(d => d.Event)
                .Where(d => d.UserID == user.Id)
                .ToListAsync();

            return View(donations);
        }
        [Authorize]
        [Authorize]
        public IActionResult MyDonations(string search)
        {
            var userId = _userManager.GetUserId(User);

            // Truy vấn ban đầu
            var donationsQuery = _context.BloodDonations
                .Include(d => d.Event)
                .Where(d => d.UserID == userId)
                .OrderByDescending(d => d.RegistrationDate)
                .AsEnumerable(); // ⛔ EF không hỗ trợ ToString trong LINQ, nên chuyển sang bộ nhớ

            // Tìm kiếm nếu có
            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower().Trim(); // Bỏ khoảng trắng và không phân biệt hoa thường

                donationsQuery = donationsQuery.Where(d =>
                    (d.Event?.EventName != null && d.Event.EventName.ToLower().Contains(search)) ||
                    d.RegistrationDate.ToString("yyyy-MM-dd").Contains(search) ||
                    d.Status.ToString().ToLower().Contains(search)
                );
            }

            return View("MyDonations", donationsQuery.ToList());
        }


        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var donation = await _context.BloodDonations.FirstOrDefaultAsync(d => d.DonationID == id && d.UserID == user.Id);

            if (donation != null && donation.Status == BloodDonationStatus.Pending)
            {
                _context.BloodDonations.Remove(donation);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Bạn đã hủy đăng ký thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hủy đăng ký này.";
            }

            return RedirectToAction("MyDonations");
        }

        // ✅ Xử lý đăng ký hiến máu
        [HttpPost]
        public async Task<IActionResult> Register(int eventId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Kiểm tra nếu user đã đăng ký sự kiện này trước đó
            var existingDonation = await _context.BloodDonations
                .FirstOrDefaultAsync(d => d.UserID == user.Id && d.EventId == eventId);

            if (existingDonation != null)
            {
                TempData["ErrorMessage"] = "Bạn đã đăng ký sự kiện này rồi!";
                return RedirectToAction("Index");
            }

            var donation = new BloodDonation
            {
                UserID = user.Id,
                EventId = eventId,
                RegistrationDate = DateTime.Now,
                Status = BloodDonationStatus.Pending
            };

            _context.BloodDonations.Add(donation);
            await _context.SaveChangesAsync();

            // ✅ Gửi Email Xác Nhận (nếu cần)
            string subject = "Xác nhận đăng ký hiến máu";
            string body = $"Xin chào {user.FullName},<br/><br/>" +
                          $"Bạn đã đăng ký thành công sự kiện hiến máu. Hãy chờ xác nhận từ Admin.<br/><br/>" +
                          $"Cảm ơn bạn đã tham gia hiến máu nhân đạo!<br/><br/>" +
                          $"Ngày đăng ký: {DateTime.Now:dd/MM/yyyy HH:mm}";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            TempData["SuccessMessage"] = "Đăng ký hiến máu thành công! Vui lòng kiểm tra email xác nhận.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmRegistration(BloodDonationRegisterViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Lấy thông tin sự kiện
            var eventInfo = await _context.BloodDonationEvents.FindAsync(model.EventID);
            if (eventInfo == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sự kiện.";
                return RedirectToAction("Index");
            }

            if (eventInfo.IsLocked)
            {
                TempData["ErrorMessage"] = "Sự kiện này đã bị khóa và không thể đăng ký thêm.";
                return RedirectToAction("Index");
            }

            if (eventInfo.Date.Date < DateTime.Now.Date)
            {
                TempData["ErrorMessage"] = "Không thể đăng ký sự kiện đã diễn ra.";
                return RedirectToAction("Index");
            }

            // Kiểm tra đã đăng ký sự kiện chưa
            var existingDonation = await _context.BloodDonations
                .FirstOrDefaultAsync(d => d.UserID == user.Id && d.EventId == model.EventID);
            if (existingDonation != null)
            {
                TempData["ErrorMessage"] = "Bạn đã đăng ký sự kiện này rồi!";
                return RedirectToAction("Index");
            }

            // Kiểm tra khoảng cách 90 ngày giữa các lần hiến máu
            var latestDonation = await _context.BloodDonations
                .Include(d => d.Event)
                .Where(d => d.UserID == user.Id && d.Status == BloodDonationStatus.Completed)
                .OrderByDescending(d => d.Event.Date)
                .FirstOrDefaultAsync();

            if (latestDonation != null && (eventInfo.Date - latestDonation.Event.Date).TotalDays < 90)
            {
                TempData["ErrorMessage"] = "Bạn chỉ được hiến máu mỗi 90 ngày. Vui lòng thử lại sau.";
                return RedirectToAction("Index");
            }

            // ✅ Ràng buộc nhóm máu chỉ 1 lần
            if (string.IsNullOrEmpty(user.BloodType) || user.BloodType == "Unknown")
            {
                user.BloodType = model.BloodType;
                user.IsBloodTypeLocked = true;
            }
            else if (user.IsBloodTypeLocked && user.BloodType != model.BloodType)
            {
                TempData["ErrorMessage"] = "Bạn không thể thay đổi nhóm máu sau khi đã đăng ký lần đầu.";
                return RedirectToAction("RegisterForm", new { eventId = model.EventID });
            }

            // ✅ Upload ảnh BHYT
            if (model.HealthInsuranceImage != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(model.HealthInsuranceImage.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/insurance", fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await model.HealthInsuranceImage.CopyToAsync(stream);
                }
                user.HealthInsuranceImagePath = "/uploads/insurance/" + fileName;
            }

            // ✅ Upload hồ sơ khám bệnh
            if (model.MedicalDocument != null)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(model.MedicalDocument.FileName);
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/medical", fileName);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await model.MedicalDocument.CopyToAsync(stream);
                }
                user.MedicalDocumentPath = "/uploads/medical/" + fileName;
            }

            // ✅ Cập nhật thông tin người dùng
            await _userManager.UpdateAsync(user);

            // ✅ Tạo bản ghi đăng ký hiến máu
            var donation = new BloodDonation
            {
                UserID = user.Id,
                EventId = model.EventID,
                RegistrationDate = eventInfo.Date,
                Status = BloodDonationStatus.Pending,
                BloodType = user.BloodType
            };

            _context.BloodDonations.Add(donation);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng chờ xác nhận từ Admin.";
            return RedirectToAction("Index");
        }







    }
}
