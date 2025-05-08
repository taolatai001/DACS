using Microsoft.AspNetCore.Mvc;
using CSDL.Data;
using Microsoft.EntityFrameworkCore;

namespace CSDL.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BloodEventApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BloodEventApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // /api/BloodEventApi/upcoming
        [HttpGet("upcoming")]
        public async Task<IActionResult> GetUpcomingEvents()
        {
            var today = DateTime.Today;
            var next7Days = today.AddDays(7);

            var events = await _context.BloodDonationEvents
                .Where(e => e.Date >= today && e.Date <= next7Days)
                .Select(e => new
                {
                    e.EventName,
                    e.Date,
                    e.Location,
                    Time = e.Description,
                    e.Latitude,
                    e.Longitude
                })
                .ToListAsync();

            return Ok(events);
        }
    }
}
