using System;
using System.ComponentModel.DataAnnotations;

namespace CSDL.Models
{
    public class BloodDonationEvent
    {
        [Key]
        public int EventID { get; set; }

        [Required(ErrorMessage = "Tên sự kiện không được để trống.")]
        public string EventName { get; set; }

        [Required(ErrorMessage = "Ngày tổ chức không được để trống.")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Địa điểm không được để trống.")]
        public string Location { get; set; }

        public bool IsLocked { get; set; } = false;

        public string Description { get; set; }

        // ✅ Thêm toạ độ cho bản đồ
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
