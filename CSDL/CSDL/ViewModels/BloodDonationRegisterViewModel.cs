using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CSDL.ViewModels
{
    public class BloodDonationRegisterViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        public string PhoneNumber { get; set; }

        public string BloodType { get; set; } = "Unknown";
        public bool IsBloodTypeLocked { get; set; }

        // ✅ Chỉ giữ lại 2 file upload
        public IFormFile HealthInsuranceImage { get; set; }      // Ảnh BHYT
        public IFormFile MedicalDocument { get; set; }           // Hồ sơ khám bệnh

        public int EventID { get; set; }
    }
}
