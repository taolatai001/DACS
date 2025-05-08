using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace CSDL.ViewModels
{
    public class CompleteProfileViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
        [Phone]
        public string PhoneNumber { get; set; }

        public string? Gender { get; set; }

        public string? Address { get; set; }

        public string? BloodType { get; set; }
        public IFormFile? HealthInsuranceImage { get; set; }
        public IFormFile? MedicalDocument { get; set; }

        public string? HealthInsuranceImagePath { get; set; }
        public string? MedicalDocumentPath { get; set; }

    }
}
