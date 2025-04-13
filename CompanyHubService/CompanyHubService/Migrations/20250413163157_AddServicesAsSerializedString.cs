using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CompanyHubService.Migrations
{
    /// <inheritdoc />
    public partial class AddServicesAsSerializedString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Check if the foreign key exists before attempting to drop it
            // If the foreign key does not exist, this block can be safely removed
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Services_ProjectRequests_ProjectRequestRequestId')
                BEGIN
                    ALTER TABLE [Services] DROP CONSTRAINT [FK_Services_ProjectRequests_ProjectRequestRequestId];
                END
            ");

            // Drop the index if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Services_ProjectRequestRequestId')
                BEGIN
                    DROP INDEX [IX_Services_ProjectRequestRequestId] ON [Services];
                END
            ");

            // Drop the column if it exists
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ProjectRequestRequestId' AND Object_ID = Object_ID(N'Services'))
                BEGIN
                    ALTER TABLE [Services] DROP COLUMN [ProjectRequestRequestId];
                END
            ");

            // Add the new column
            migrationBuilder.AddColumn<string>(
                name: "Services",
                table: "ProjectRequests",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the new column
            migrationBuilder.DropColumn(
                name: "Services",
                table: "ProjectRequests");

            // Re-add the dropped column
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectRequestRequestId",
                table: "Services",
                type: "uniqueidentifier",
                nullable: true);

            // Recreate the index
            migrationBuilder.CreateIndex(
                name: "IX_Services_ProjectRequestRequestId",
                table: "Services",
                column: "ProjectRequestRequestId");

            // Recreate the foreign key
            migrationBuilder.AddForeignKey(
                name: "FK_Services_ProjectRequests_ProjectRequestRequestId",
                table: "Services",
                column: "ProjectRequestRequestId",
                principalTable: "ProjectRequests",
                principalColumn: "RequestId");
        }
    }
}
