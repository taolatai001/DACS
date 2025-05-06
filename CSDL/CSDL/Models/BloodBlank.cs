using System.ComponentModel.DataAnnotations;

namespace CSDL.Models
{
    public class BloodBank
    {
        public int BloodBankID { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public string BloodType { get; set; }

        public int Quantity { get; set; }
    }
}