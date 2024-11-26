using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyHubService.Migrations
{
    /// <inheritdoc />
    public partial class roleIDAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RoleId",
                table: "UserCompanies",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserCompanies_RoleId",
                table: "UserCompanies",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserCompanies_AspNetRoles_RoleId",
                table: "UserCompanies",
                column: "RoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserCompanies_AspNetRoles_RoleId",
                table: "UserCompanies");

            migrationBuilder.DropIndex(
                name: "IX_UserCompanies_RoleId",
                table: "UserCompanies");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "UserCompanies");
        }
    }
}
