using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyHubService.Migrations
{
    /// <inheritdoc />
    public partial class companyProfileRerevised : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Industry",
                table: "ProjectRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectRequestRequestId",
                table: "Services",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId1",
                table: "ServiceProjects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId1",
                table: "ServiceCompanies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Percentage",
                table: "ServiceCompanies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Services_ProjectRequestRequestId",
                table: "Services",
                column: "ProjectRequestRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProjects_ProjectId1",
                table: "ServiceProjects",
                column: "ProjectId1");

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

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceProjects_Projects_ProjectId1",
                table: "ServiceProjects",
                column: "ProjectId1",
                principalTable: "Projects",
                principalColumn: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Services_ProjectRequests_ProjectRequestRequestId",
                table: "Services",
                column: "ProjectRequestRequestId",
                principalTable: "ProjectRequests",
                principalColumn: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceCompanies_Companies_CompanyId1",
                table: "ServiceCompanies");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceProjects_Projects_ProjectId1",
                table: "ServiceProjects");

            migrationBuilder.DropForeignKey(
                name: "FK_Services_ProjectRequests_ProjectRequestRequestId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_ProjectRequestRequestId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_ServiceProjects_ProjectId1",
                table: "ServiceProjects");

            migrationBuilder.DropIndex(
                name: "IX_ServiceCompanies_CompanyId1",
                table: "ServiceCompanies");

            migrationBuilder.DropColumn(
                name: "ProjectRequestRequestId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ProjectId1",
                table: "ServiceProjects");

            migrationBuilder.DropColumn(
                name: "CompanyId1",
                table: "ServiceCompanies");

            migrationBuilder.DropColumn(
                name: "Percentage",
                table: "ServiceCompanies");

            migrationBuilder.AddColumn<string>(
                name: "Industry",
                table: "ProjectRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
