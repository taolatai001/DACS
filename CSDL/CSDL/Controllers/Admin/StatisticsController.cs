using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CSDL.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CSDL.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("admin/thong-ke")]
    public class StatisticsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatisticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var totalDonations = await _context.BloodDonations.CountAsync();
            var totalUsers = await _context.BloodDonations
                .Select(d => d.UserID)
                .Distinct()
                .CountAsync();
            var totalEvents = await _context.BloodDonationEvents.CountAsync();

            var topUsers = await _context.BloodDonations
                .GroupBy(d => d.UserID)
                .Select(g => new
                {
                    UserID = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .Join(_context.Users,
                      g => g.UserID,
                      u => u.Id,
                      (g, u) => new
                      {
                          FullName = u.FullName,
                          Count = g.Count
                      })
                .ToListAsync();

            ViewBag.TotalDonations = totalDonations;
            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalEvents = totalEvents;
            ViewBag.TopUsers = topUsers;
            var donationsByMonth = await _context.BloodDonations
    .GroupBy(d => d.RegistrationDate.Month)
    .Select(g => new
    {
        Month = g.Key,
        Count = g.Count()
    })
    .OrderBy(g => g.Month)
    .ToListAsync();

            ViewBag.DonationsByMonth = System.Text.Json.JsonSerializer.Serialize(donationsByMonth);

            return View();
        }
    }
}