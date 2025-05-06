using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CSDL.Models;

namespace CSDL.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentID { get; set; }

        [Required]
        public string UserID { get; set; }  // ✅ Đổi từ int -> string

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string Status { get; set; } // Pending, Confirmed, Cancelled

        [ForeignKey("UserID")]
        public User User { get; set; }
    }
}
