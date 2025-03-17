using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyHubService.Migrations
{
    /// <inheritdoc />
    public partial class ProjectTableUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ContactInfo",
                table: "Companies",
                newName: "Phone");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Companies");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "Companies",
                newName: "ContactInfo");
        }
    }
}
