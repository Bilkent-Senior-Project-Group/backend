using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyHubService.Migrations
{
    /// <inheritdoc />
    public partial class MakeOtherCompanyNameNullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "OtherCompanyName",
                table: "ProjectCompanies",
                type: "nvarchar(max)",
                nullable: true, // 👈 asıl değişiklik bu
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "OtherCompanyName",
                table: "ProjectCompanies",
                type: "nvarchar(max)",
                nullable: false, // rollback için
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

    }
}
