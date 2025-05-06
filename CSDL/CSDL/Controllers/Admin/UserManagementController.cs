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

        // ✅ 1. Danh sách người dùng
        public async Task<IActionResult> Index(string search)
        {
            var users = await _userManager.Users.ToListAsync();  // Lấy tất cả người dùng từ UserManager
            var userRoles = new Dictionary<string, string>();

            // Tạo Dictionary để lưu vai trò của người dùng
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRoles[user.Id] = roles.Any() ? string.Join(", ", roles) : "Người dùng";  // Lưu vai trò vào Dictionary
            }

            // Lọc người dùng theo tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                users = users.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search) ||
                    userRoles[u.Id].Contains(search)).ToList();
            }

            // Truyền dữ liệu vào View
            ViewData["UserRoles"] = userRoles;
            return View(users);  // Trả về View với người dùng và vai trò
        }


        // ✅ 2. Xóa người dùng
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);
            TempData["SuccessMessage"] = "Xóa người dùng thành công.";
            return RedirectToAction("Index");
        }

        // ✅ 3. Hiển thị form chỉnh sửa người dùng
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
                Role = roles.FirstOrDefault() ?? "Người dùng"
            };

            return View(model);
        }

        // ✅ 4. Cập nhật thông tin người dùng
        [HttpPost]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // ✅ Kiểm tra Email có bị trùng không
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null && existingUser.Id != model.Id)
            {
                ModelState.AddModelError("Email", "Email này đã tồn tại trong hệ thống.");
                return View(model);
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.UserName = model.Email;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cập nhật thông tin thất bại.");
                return View(model);
            }

            // ✅ Cập nhật vai trò
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);

            TempData["SuccessMessage"] = "Cập nhật thông tin thành công.";
            return RedirectToAction("Index");
        }
    }
}
