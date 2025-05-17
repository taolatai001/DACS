using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CSDL.Models
{
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        [Required]
        public string Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // ✅ Thông tin người nhận
        public string UserId { get; set; }  // Gửi riêng cho từng User

        [ForeignKey("UserId")]
        public User User { get; set; }      // ✅ Thuộc tính điều hướng để Include User và lấy FullName

        [Required]
        public string Title { get; set; }   // Tiêu đề thông báo

        [Required]
        public string Link { get; set; }    // Link đính kèm (nếu có)

        public bool IsRead { get; set; } = false;  // Trạng thái đã đọc hay chưa
    }
}
