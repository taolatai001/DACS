using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CSDL.ViewModels;
using CSDL.Models;
using System.Security.Claims;

namespace CSDL.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                {
                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                        return RedirectToAction("Dashboard", "Admin");

                    return RedirectToAction("Index", "Home");
                }
            }

            TempData["ErrorMessage"] = "Email hoặc mật khẩu không đúng!";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ExternalLogin(string provider, string returnUrl = "/")
        {
            var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/")
        {
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["ErrorMessage"] = "Không thể đăng nhập bằng Google.";
                return RedirectToAction("Login");
            }

            // Đã từng liên kết
            var signInResult = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
            if (signInResult.Succeeded)
            {
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tài khoản người dùng.";
                    return RedirectToAction("Login");
                }

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                    return RedirectToAction("Dashboard", "Admin");

                // ✅ Kiểm tra thiếu thông tin → chuyển đến trang hoàn thiện hồ sơ
                if (string.IsNullOrWhiteSpace(user.FullName?.Trim()) ||
                    string.IsNullOrWhiteSpace(user.PhoneNumber?.Trim()) ||
                    string.IsNullOrWhiteSpace(user.Gender?.Trim()) ||
                    string.IsNullOrWhiteSpace(user.Address?.Trim()))
                {
                    return RedirectToAction("CompleteProfile", new { email = user.Email });
                }

                return RedirectToAction("Index", "Home");
            }

            // Chưa từng liên kết Google → tạo tài khoản
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var fullName = info.Principal.FindFirstValue(ClaimTypes.Name);

            if (!string.IsNullOrEmpty(email))
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    user = new User
                    {
                        UserName = email,
                        Email = email,
                        FullName = fullName ?? "",
                        PhoneNumber = "", // Để ép cập nhật
                        Gender = null,
                        Address = null
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, "User");
                        await _userManager.AddLoginAsync(user, info);
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToAction("CompleteProfile", new { email = user.Email });
                    }
                }
                else
                {
                    // Đã có user → liên kết Google
                    await _userManager.AddLoginAsync(user, info);
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    if (await _userManager.IsInRoleAsync(user, "Admin"))
                        return RedirectToAction("Dashboard", "Admin");

                    if (string.IsNullOrWhiteSpace(user.FullName?.Trim()) ||
                        string.IsNullOrWhiteSpace(user.PhoneNumber?.Trim()) ||
                        string.IsNullOrWhiteSpace(user.Gender?.Trim()) ||
                        string.IsNullOrWhiteSpace(user.Address?.Trim()))
                    {
                        return RedirectToAction("CompleteProfile", new { email = user.Email });
                    }

                    return RedirectToAction("Index", "Home");
                }
            }

            TempData["ErrorMessage"] = "Không thể lấy thông tin email từ Google.";
            return RedirectToAction("Login");
        }


        [HttpGet]
        public async Task<IActionResult> CompleteProfile(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null) return NotFound();

            return View(new CompleteProfileViewModel
            {
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                Gender = user.Gender,
                Address = user.Address,
                BloodType = user.BloodType,
                HealthInsuranceImagePath = user.HealthInsuranceImagePath,
                MedicalDocumentPath = user.MedicalDocumentPath
            });
        }

        [HttpPost]
        public async Task<IActionResult> CompleteProfile(CompleteProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                foreach (var entry in ModelState)
                {
                    foreach (var error in entry.Value.Errors)
                    {
                        Console.WriteLine($"Lỗi tại '{entry.Key}': {error.ErrorMessage}");
                    }
                }

                // Giữ lại path cũ nếu có
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    model.HealthInsuranceImagePath = existingUser.HealthInsuranceImagePath;
                    model.MedicalDocumentPath = existingUser.MedicalDocumentPath;
                }

                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.Gender = model.Gender;
            user.Address = model.Address;
            user.BloodType = model.BloodType ?? "Unknown";

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            // Ảnh BHYT
            if (model.HealthInsuranceImage != null && model.HealthInsuranceImage.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(model.HealthInsuranceImage.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.HealthInsuranceImage.CopyToAsync(stream);
                }

                user.HealthInsuranceImagePath = "/uploads/" + fileName;
            }

            // Hồ sơ khám bệnh
            if (model.MedicalDocument != null && model.MedicalDocument.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(model.MedicalDocument.FileName);
                var filePath = Path.Combine(uploadsPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.MedicalDocument.CopyToAsync(stream);
                }

                user.MedicalDocumentPath = "/uploads/" + fileName;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Index", "Home");
            }

            // Nếu thất bại, truyền lại đường dẫn hiện tại để hiển thị
            model.HealthInsuranceImagePath = user.HealthInsuranceImagePath;
            model.MedicalDocumentPath = user.MedicalDocumentPath;

            ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật.");
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied() => View();
    }
}
