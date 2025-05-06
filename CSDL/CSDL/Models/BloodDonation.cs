using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSDL.Models
{
    public enum BloodDonationStatus
    {
        Pending,   // Chờ xác nhận
        Completed, // Đã hiến máu
        Cancelled  // Hủy đăng ký
    }

    public class BloodDonation
    {
        [Key]
        public int DonationID { get; set; }

        [Required]
        public string UserID { get; set; }

        [ForeignKey("UserID")]
        public User User { get; set; }  // ✅ Liên kết với bảng AspNetUsers

        public int EventId { get; set; }

        [ForeignKey("EventId")]
        public BloodDonationEvent Event { get; set; }

        [Required]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [Required]
        public string BloodType { get; set; } = "Unknown"; // ✅ Thêm cột này

        public BloodDonationStatus Status { get; set; } = BloodDonationStatus.Pending;
    }
}
