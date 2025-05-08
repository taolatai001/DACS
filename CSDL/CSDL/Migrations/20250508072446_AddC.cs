using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CSDL.Migrations
{
    /// <inheritdoc />
    public partial class AddC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "BloodDonationEvents",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "BloodDonationEvents",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "BloodDonationEvents");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "BloodDonationEvents");
        }
    }
}
