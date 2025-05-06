using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CSDL.Migrations
{
    /// <inheritdoc />
    public partial class AddIsBloodTypeLockedToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsBloodTypeLocked",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsBloodTypeLocked",
                table: "AspNetUsers");
        }
    }
}
