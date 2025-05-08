using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using CSDL.Models;
using CSDL.ViewModels;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CSDL.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagementController(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ✅ 1. Danh sách người dùng (có tìm kiếm)
        public async Task<IActionResult> Index(string search)
        {
            var users = await _userManager.Users.ToListAsync();
            var userRoles = new Dictionary<string, string>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.Any() ? string.Join(", ", roles) : "Người dùng";
            }

            if (!string.IsNullOrEmpty(search))
            {
                search = search.ToLower();
                users = users.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(search)) ||
                    (u.Email != null && u.Email.ToLower().Contains(search)) ||
                    userRoles[u.Id].ToLower().Contains(search)
                ).ToList();
            }

            ViewData["UserRoles"] = userRoles;
            return View(users);
        }

        // ✅ 2. Xem chi tiết người dùng
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // ✅ 3. Xóa người dùng
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);
            TempData["SuccessMessage"] = "🗑️ Xóa người dùng thành công.";
            return RedirectToAction("Index");
        }

        // ✅ 4. Hiển thị form chỉnh sửa
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var model = new EditUserViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = roles.FirstOrDefault() ?? "User"
            };

            LoadRolesToViewBag();
            return View(model);
        }

        // ✅ 5. Cập nhật người dùng
        [HttpPost]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                LoadRolesToViewBag();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // Kiểm tra trùng email
            var duplicateEmailUser = await _userManager.FindByEmailAsync(model.Email);
            if (duplicateEmailUser != null && duplicateEmailUser.Id != user.Id)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                LoadRolesToViewBag();
                return View(model);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cập nhật thất bại.");
                LoadRolesToViewBag();
                return View(model);
            }

            // Cập nhật vai trò
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);

            TempData["SuccessMessage"] = "✅ Cập nhật thông tin người dùng thành công.";
            return RedirectToAction("Index");
        }

        // ✅ Phương thức dùng lại để load danh sách role
        private void LoadRolesToViewBag()
        {
            ViewBag.AllRoles = _roleManager.Roles.Select(r => r.Name).ToList();
        }
    }
}
