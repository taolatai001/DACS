using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CSDL.Models;

namespace CSDL.Models
{
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        [Required]

        public string Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;


    }
}
