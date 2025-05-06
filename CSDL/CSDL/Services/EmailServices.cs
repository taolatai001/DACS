using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CSDL.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string message, bool isHtml = true, string cc = null, string bcc = null)
        {
            try
            {
                // ✅ Lấy thông tin cấu hình từ appsettings.json
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var port = int.Parse(_configuration["EmailSettings:Port"]);
                var senderEmail = _configuration["EmailSettings:SenderEmail"];
                var senderPassword = _configuration["EmailSettings:SenderPassword"];
                var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"]);

                using (var client = new SmtpClient(smtpServer, port))
                {
                    client.Credentials = new NetworkCredential(senderEmail, senderPassword);
                    client.EnableSsl = enableSsl;
                    client.UseDefaultCredentials = false; // ✅ Đảm bảo sử dụng thông tin đăng nhập SMTP

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(senderEmail);
                        mailMessage.To.Add(toEmail);
                        mailMessage.Subject = subject;
                        mailMessage.Body = message;
                        mailMessage.IsBodyHtml = isHtml; // ✅ Cho phép gửi email HTML

                        // ✅ Thêm CC & BCC nếu có
                        if (!string.IsNullOrEmpty(cc)) mailMessage.CC.Add(cc);
                        if (!string.IsNullOrEmpty(bcc)) mailMessage.Bcc.Add(bcc);

                        await client.SendMailAsync(mailMessage);
                        _logger.LogInformation($"📧 Email gửi thành công đến {toEmail} với tiêu đề: {subject}");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Lỗi khi gửi email đến {toEmail}: {ex.Message}");
                return false;
            }
        }
    }
}
