using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyHubService.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyIdToUserClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "AspNetUserClaims",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "AspNetUserClaims",
                type: "nvarchar(34)",
                maxLength: 34,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_CompanyId",
                table: "AspNetUserClaims",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserClaims_Companies_CompanyId",
                table: "AspNetUserClaims",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "CompanyId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserClaims_Companies_CompanyId",
                table: "AspNetUserClaims");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserClaims_CompanyId",
                table: "AspNetUserClaims");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AspNetUserClaims");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "AspNetUserClaims");
        }
    }
}
