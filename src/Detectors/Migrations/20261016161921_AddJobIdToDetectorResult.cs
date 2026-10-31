using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaTrust.Detectors.Migrations
{
    /// <inheritdoc />
    public partial class AddJobIdToDetectorResult : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "JobId",
                table: "DetectorResults",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "JobId",
                table: "DetectorResults");
        }
    }
}
