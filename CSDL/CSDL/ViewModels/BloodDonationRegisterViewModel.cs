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

        [Required(ErrorMessage = "Vui lòng chọn nhóm máu")]
        public string BloodType { get; set; }

        public bool IsBloodTypeLocked { get; set; }

        [Required(ErrorMessage = "Vui lòng đính kèm ảnh thẻ BHYT")]
        public IFormFile HealthInsuranceImage { get; set; }

        [Required(ErrorMessage = "Vui lòng đính kèm hồ sơ khám bệnh")]
        public IFormFile MedicalDocument { get; set; }

        public int EventID { get; set; }

        // Không cần bắt buộc vì được sinh sau khi lưu ảnh
        public string? HealthInsuranceImagePath { get; set; }
        public string? MedicalDocumentPath { get; set; }
    }
}
