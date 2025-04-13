using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyHubService.Migrations
{
    /// <inheritdoc />
    public partial class conflictingForeignKeyResolved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceProjects_Projects_ProjectId1",
                table: "ServiceProjects");

            migrationBuilder.DropIndex(
                name: "IX_ServiceProjects_ProjectId1",
                table: "ServiceProjects");

            migrationBuilder.DropColumn(
                name: "ProjectId1",
                table: "ServiceProjects");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId1",
                table: "ServiceProjects",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceProjects_ProjectId1",
                table: "ServiceProjects",
                column: "ProjectId1");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceProjects_Projects_ProjectId1",
                table: "ServiceProjects",
                column: "ProjectId1",
                principalTable: "Projects",
                principalColumn: "ProjectId");
        }
    }
}
