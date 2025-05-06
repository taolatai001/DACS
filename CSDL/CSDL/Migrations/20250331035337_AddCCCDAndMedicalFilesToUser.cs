using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CSDL.Migrations
{
    /// <inheritdoc />
    public partial class AddCCCDAndMedicalFilesToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CCCD",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HealthInsuranceImagePath",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MedicalDocumentPath",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CCCD",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "HealthInsuranceImagePath",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "MedicalDocumentPath",
                table: "AspNetUsers");
        }
    }
}
