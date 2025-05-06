using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using CSDL.ViewModels;
using CSDL.Models;
using CSDL.Services; // ✅ Thêm EmailService để gửi email xác nhận

namespace CSDL.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly EmailService _emailService; // ✅ Khai báo EmailService

        public AccountController(UserManager<User> userManager, SignInManager<User> signInManager, EmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService; // ✅ Gán service email
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

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
                    {
                        return RedirectToAction("Dashboard", "Admin"); // ✅ Chuyển đến trang Admin Dashboard
                    }
                    return RedirectToAction("Index", "Home"); // ✅ Chuyển đến trang User Home
                }
            }

            TempData["ErrorMessage"] = "Email hoặc mật khẩu không đúng!";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // ✅ Kiểm tra số điện thoại hợp lệ (phải có ít nhất 9 số)
            if (string.IsNullOrWhiteSpace(model.PhoneNumber) || model.PhoneNumber.Length < 9)
            {
                ModelState.AddModelError("", "Số điện thoại không hợp lệ. Vui lòng nhập ít nhất 9 chữ số.");
                return View(model);
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");

                // ✅ Gửi email xác nhận đăng ký
                try
                {
                    string subject = "Chào mừng bạn đến với hệ thống hiến máu!";
                    string message = $"<p>Xin chào <strong>{model.FullName}</strong>,</p>" +
                                     "<p>Cảm ơn bạn đã đăng ký tài khoản. Bạn có thể đăng nhập ngay tại hệ thống.</p>" +
                                     "<p>Chúc bạn một ngày tốt lành!</p>";

                    await _emailService.SendEmailAsync(model.Email, subject, message);
                }
                catch (Exception ex)
                {
                    // ✅ Ghi log lỗi nếu gửi email thất bại
                    TempData["WarningMessage"] = "Đăng ký thành công nhưng không thể gửi email. Vui lòng kiểm tra sau.";
                    Console.WriteLine($"[Email Error]: {ex.Message}");
                }

                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng kiểm tra email để nhận thông tin.";
                return RedirectToAction("Login");
            }

            // ✅ Hiển thị lỗi rõ ràng hơn
            foreach (var error in result.Errors)
            {
                string message = error.Code switch
                {
                    "PasswordRequiresDigit" => "Mật khẩu phải chứa ít nhất một chữ số (0-9).",
                    "PasswordRequiresLower" => "Mật khẩu phải chứa ít nhất một chữ cái thường (a-z).",
                    "PasswordRequiresUpper" => "Mật khẩu phải chứa ít nhất một chữ cái in hoa (A-Z).",
                    "PasswordRequiresNonAlphanumeric" => "Mật khẩu phải chứa ít nhất một ký tự đặc biệt (@, #, !, *).",
                    "PasswordTooShort" => "Mật khẩu phải có ít nhất 6 ký tự.",
                    "DuplicateUserName" => "Email này đã được đăng ký.",
                    "InvalidEmail" => "Email không hợp lệ.",
                    _ => error.Description
                };

                ModelState.AddModelError("", message);
            }

            return View(model);
        }
        private string GenerateRandomPassword(int length = 10)
        {
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string specials = "!@#$%^&*";
            var random = new Random();

            // Bắt buộc có ít nhất 1 ký tự của mỗi nhóm
            var password = new List<char>
    {
        uppercase[random.Next(uppercase.Length)],
        lowercase[random.Next(lowercase.Length)],
        digits[random.Next(digits.Length)],
        specials[random.Next(specials.Length)]
    };

            // Thêm các ký tự còn lại ngẫu nhiên từ tất cả nhóm
            var allChars = uppercase + lowercase + digits + specials;
            while (password.Count < length)
            {
                password.Add(allChars[random.Next(allChars.Length)]);
            }

            // Trộn vị trí ngẫu nhiên
            return new string(password.OrderBy(_ => random.Next()).ToArray());
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                // ✅ Đăng xuất người dùng ngay sau khi đổi mật khẩu thành công
                await _signInManager.SignOutAsync();

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login", "Account");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }


        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }
        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy email.";
                return View(model);
            }

            // Tạo mật khẩu ngẫu nhiên
            var newPassword = GenerateRandomPassword();

            // Reset mật khẩu
            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

            if (result.Succeeded)
            {
                // Gửi mật khẩu mới qua email
                await _emailService.SendEmailAsync(user.Email, "Mật khẩu mới từ hệ thống Hiến Máu",
                    $"Chào {user.UserName},\n\nMật khẩu mới của bạn là: <strong>{newPassword}</strong><br>Bạn có thể đăng nhập và đổi lại mật khẩu trong phần tài khoản.");

                TempData["SuccessMessage"] = "Mật khẩu mới đã được gửi qua email.";
                return RedirectToAction("Login");
            }

            TempData["ErrorMessage"] = "Đã xảy ra lỗi khi đặt lại mật khẩu.";
            return View(model);
        }
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
    
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
