using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSDL.Models
{
    [Table("AspNetUsers")]  // ✅ Đảm bảo đúng bảng của ASP.NET Identity
    public class User : IdentityUser
    {
        [Required(ErrorMessage = "Họ và tên không được để trống.")]
        [MaxLength(100, ErrorMessage = "Họ và tên không được vượt quá 100 ký tự.")]
        public string FullName { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        [MaxLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự.")]

    
        public override string PhoneNumber { get; set; } = "0000000000";
        [MaxLength(10)]
        public string BloodType { get; set; } = "Unknown";

        public string? HealthInsuranceImagePath { get; set; }
        public string? MedicalDocumentPath { get; set; }

        public bool IsBloodTypeLocked { get; set; } = false;

        // ✅ Thêm các thuộc tính mới để nhập sau khi đăng nhập bằng Google
        [MaxLength(10)]
        public string? Gender { get; set; }

        [MaxLength(200)]
        public string? Address { get; set; }
    }
}
