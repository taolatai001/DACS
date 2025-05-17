using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using CSDL.Data;

namespace CSDL.ViewComponents
{
    public class NotificationCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public NotificationCountViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = ViewContext.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            int count = 0;

            if (!string.IsNullOrEmpty(userId))
            {
                count = _context.Notifications.Count(n => n.UserId == userId && !n.IsRead);
            }

            return View(count);
        }
    }
}
