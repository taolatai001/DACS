using System.ComponentModel.DataAnnotations;

namespace CSDL.ViewModels
{
    public class ConfirmRegistrationViewModel
    {
        public int EventID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhóm máu.")]
        public string BloodType { get; set; } = "Unknown";
    }
}
