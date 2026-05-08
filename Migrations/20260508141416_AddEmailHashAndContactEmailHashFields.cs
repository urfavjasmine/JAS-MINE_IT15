using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JAS_MINE_IT15.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailHashAndContactEmailHashFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailHash",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmailHash",
                table: "Barangays",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_EmailHash",
                table: "Users",
                column: "EmailHash",
                unique: true,
                filter: "[EmailHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Barangays_ContactEmailHash",
                table: "Barangays",
                column: "ContactEmailHash",
                unique: true,
                filter: "[ContactEmailHash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_EmailHash",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Barangays_ContactEmailHash",
                table: "Barangays");

            migrationBuilder.DropColumn(
                name: "EmailHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ContactEmailHash",
                table: "Barangays");
        }
    }
}
