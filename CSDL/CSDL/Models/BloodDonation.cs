using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSDL.Models
{
    // ✅ Thêm trạng thái Rejected để xử lý trường hợp không đủ điều kiện hiến máu
    public enum BloodDonationStatus
    {
        Pending,     // Chờ xác nhận
        Completed,   // Đã hiến máu
        Cancelled,   // Tự hủy đăng ký (nếu bạn muốn xử lý)
        Rejected     // Bị từ chối (không đủ điều kiện sức khỏe, giấy tờ không hợp lệ...)
    }

    public class BloodDonation
    {
        [Key]
        public int DonationID { get; set; }

        [Required]
        public string UserID { get; set; }

        [ForeignKey("UserID")]
        public User User { get; set; }  // ✅ Liên kết với bảng người dùng

        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public BloodDonationEvent Event { get; set; }

        [Required]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [Required]
        public string BloodType { get; set; } = "Unknown"; // ✅ Có thể dùng trong thống kê

        // ✅ Mặc định trạng thái là Pending (chờ xác nhận)
        public BloodDonationStatus Status { get; set; } = BloodDonationStatus.Pending;
    }
}
