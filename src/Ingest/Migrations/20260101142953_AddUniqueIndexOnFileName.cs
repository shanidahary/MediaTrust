using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediaTrust.Ingest.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexOnFileName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "MediaItems");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItems_FileName",
                table: "MediaItems",
                column: "FileName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MediaItems_FileName",
                table: "MediaItems");

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "MediaItems",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
