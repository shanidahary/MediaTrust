using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaTrust.Orchestrator.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdatedAtUtcToAnalysisJo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAtUtc",
                table: "AnalysisJobs",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "AnalysisJobs");
        }
    }
}
