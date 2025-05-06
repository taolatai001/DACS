using Microsoft.AspNetCore.Mvc;
using CSDL.Data;
using System;
using System.Linq;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Dashboard()
    {
        var today = DateTime.Today;
        var next30Days = today.AddDays(30);

        // 👤 Tổng số người dùng
        ViewBag.UserCount = _context.Users.Count();

        // 💉 Tổng số lượt hiến máu
        ViewBag.TotalDonations = _context.BloodDonations.Count();

        // 📅 Số sự kiện trong 30 ngày tới
        ViewBag.Upcoming30DaysEvents = _context.BloodDonationEvents
                                                .Where(e => e.Date >= today && e.Date <= next30Days)
                                                .Count();

        // 🩸 Người hiến máu lần đầu (có đúng 1 lượt hiến máu)
        ViewBag.FirstTimeDonors = _context.BloodDonations
                                          .GroupBy(d => d.UserID)
                                          .Where(g => g.Count() == 1)
                                          .Count();

        return View();
    }
}
