using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyHubService.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanyInvitationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceCompanies_Companies_CompanyId1",
                table: "ServiceCompanies");

            migrationBuilder.DropIndex(
                name: "IX_ServiceCompanies_CompanyId1",
                table: "ServiceCompanies");

            migrationBuilder.DropColumn(
                name: "CompanyId1",
                table: "ServiceCompanies");

            migrationBuilder.CreateTable(
                name: "CompanyInvitations",
                columns: table => new
                {
                    InvitationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Accepted = table.Column<bool>(type: "bit", nullable: false),
                    Rejected = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyInvitations", x => x.InvitationId);
                    table.ForeignKey(
                        name: "FK_CompanyInvitations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "CompanyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyInvitations_CompanyId",
                table: "CompanyInvitations",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyInvitations");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId1",
                table: "ServiceCompanies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceCompanies_CompanyId1",
                table: "ServiceCompanies",
                column: "CompanyId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceCompanies_Companies_CompanyId1",
                table: "ServiceCompanies",
                column: "CompanyId1",
                principalTable: "Companies",
                principalColumn: "CompanyId");
        }
    }
}
