using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CSDL.Migrations
{
    /// <inheritdoc />
    public partial class AddIsLockedToEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLocked",
                table: "BloodDonationEvents",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLocked",
                table: "BloodDonationEvents");
        }
    }
}
