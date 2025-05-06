using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CSDL.Models;

namespace CSDL.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<BloodDonation> BloodDonations { get; set; }
        public DbSet<BloodBank> BloodBanks { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<BloodDonationEvent> BloodDonationEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // ✅ Gọi base để Identity tạo bảng

            modelBuilder.Entity<BloodDonation>().HasOne(b => b.User).WithMany().HasForeignKey(b => b.UserID);
            modelBuilder.Entity<Appointment>().HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserID);

        }
    }
}
