using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CSDL.Models;
using CSDL.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.IO;
using System.Text.Json;

namespace CSDL.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class BloodDonationEventController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BloodDonationEventController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString)
        {
            var events = from e in _context.BloodDonationEvents select e;

            if (!string.IsNullOrEmpty(searchString))
            {
                events = events.Where(e =>
                    e.EventName.Contains(searchString) ||
                    e.Location.Contains(searchString) ||
                    e.Date.ToString().Contains(searchString));
            }

            return View(await events.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(BloodDonationEvent bloodDonationEvent)
        {
            if (bloodDonationEvent.Date < DateTime.Today)
            {
                ModelState.AddModelError("Date", "Không thể tạo sự kiện với ngày trong quá khứ.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                foreach (var error in errors)
                {
                    Console.WriteLine("🚨 Lỗi ModelState: " + error);
                }

                TempData["ErrorMessage"] = "Lưu sự kiện không thành công. Vui lòng kiểm tra lại dữ liệu nhập!";
                return View(bloodDonationEvent);
            }

            try
            {
                // ✅ Bỏ gọi API — dùng tọa độ đã nhập từ form
                _context.BloodDonationEvents.Add(bloodDonationEvent);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Sự kiện hiến máu đã được thêm thành công.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine("🚨 Lỗi khi lưu vào database: " + ex.Message);
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi lưu vào database.";
                return View(bloodDonationEvent);
            }
        }


        [HttpPost]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var eventItem = await _context.BloodDonationEvents.FindAsync(id);
            if (eventItem == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sự kiện.";
                return RedirectToAction("Index");
            }

            eventItem.IsLocked = !eventItem.IsLocked;
            await _context.SaveChangesAsync();

            string status = eventItem.IsLocked ? "đã bị khóa ✅" : "đã mở lại 🔓";
            TempData["SuccessMessage"] = $"Sự kiện <strong>{eventItem.EventName}</strong> {status}.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UnlockEvent(int id)
        {
            var ev = await _context.BloodDonationEvents.FindAsync(id);
            if (ev != null)
            {
                ev.IsLocked = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "🔓 Sự kiện đã được mở khóa.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ExportExcel(int id)
        {
            var eventInfo = await _context.BloodDonationEvents.FindAsync(id);
            if (eventInfo == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sự kiện.";
                return RedirectToAction("Index");
            }

            var donations = await _context.BloodDonations
                .Include(d => d.User)
                .Where(d => d.EventId == id)
                .ToListAsync();

            var stream = new MemoryStream();
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Danh sách");
                worksheet.Cell(1, 1).Value = "STT";
                worksheet.Cell(1, 2).Value = "Họ và tên";
                worksheet.Cell(1, 3).Value = "Email";
                worksheet.Cell(1, 4).Value = "SĐT";
                worksheet.Cell(1, 5).Value = "Nhóm máu";
                worksheet.Cell(1, 7).Value = "Ảnh BHYT";
                worksheet.Cell(1, 8).Value = "Hồ sơ khám bệnh";
                worksheet.Cell(1, 9).Value = "Ngày đăng ký";
                worksheet.Cell(1, 10).Value = "Trạng thái";

                int row = 2;
                int stt = 1;
                foreach (var d in donations)
                {
                    var user = d.User;
                    worksheet.Cell(row, 1).Value = stt++;
                    worksheet.Cell(row, 2).Value = user?.FullName;
                    worksheet.Cell(row, 3).Value = user?.Email;
                    worksheet.Cell(row, 4).Value = user?.PhoneNumber;
                    worksheet.Cell(row, 5).Value = d.BloodType;

                    if (!string.IsNullOrEmpty(user?.HealthInsuranceImagePath))
                    {
                        var bhytUrl = $"{Request.Scheme}://{Request.Host}{user.HealthInsuranceImagePath}";
                        var cellBhyt = worksheet.Cell(row, 7);
                        cellBhyt.Value = "Xem ảnh";
                        cellBhyt.SetHyperlink(new XLHyperlink(bhytUrl));
                    }

                    if (!string.IsNullOrEmpty(user?.MedicalDocumentPath))
                    {
                        var docUrl = $"{Request.Scheme}://{Request.Host}{user.MedicalDocumentPath}";
                        var cellDoc = worksheet.Cell(row, 8);
                        cellDoc.Value = "Xem hồ sơ";
                        cellDoc.SetHyperlink(new XLHyperlink(docUrl));
                    }

                    worksheet.Cell(row, 9).Value = d.RegistrationDate.ToString("dd/MM/yyyy");
                    worksheet.Cell(row, 10).Value = d.Status.ToString();
                    row++;
                }

                workbook.SaveAs(stream);
            }

            stream.Position = 0;
            var fileName = $"DanhSach_HienMau_{eventInfo.EventName}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var eventItem = await _context.BloodDonationEvents.FindAsync(id);
            if (eventItem == null) return NotFound();

            return View(eventItem);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(BloodDonationEvent model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ thông tin!";
                return View(model);
            }

            if (model.Date.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Không thể cập nhật sự kiện về ngày trong quá khứ!";
                return View(model);
            }

            try
            {
                var eventItem = await _context.BloodDonationEvents.FindAsync(model.EventID);
                if (eventItem == null) return NotFound();

                eventItem.EventName = model.EventName;
                eventItem.Date = model.Date;
                eventItem.Location = model.Location;
                eventItem.Description = model.Description;

                // ✅ Dùng tọa độ nhập từ form
                eventItem.Latitude = model.Latitude;
                eventItem.Longitude = model.Longitude;

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Sự kiện đã được cập nhật thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine("🚨 Lỗi khi cập nhật sự kiện: " + ex.Message);
                TempData["ErrorMessage"] = "Cập nhật sự kiện không thành công. Vui lòng thử lại!";
                return View(model);
            }
        }


     

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var bloodDonationEvent = await _context.BloodDonationEvents.FindAsync(id);
            if (bloodDonationEvent != null)
            {
                _context.BloodDonationEvents.Remove(bloodDonationEvent);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"🗑 Đã xóa sự kiện \"{bloodDonationEvent.EventName}\" thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "❌ Không tìm thấy sự kiện cần xóa.";
            }

            return RedirectToAction("Index");
        }
    }
}
