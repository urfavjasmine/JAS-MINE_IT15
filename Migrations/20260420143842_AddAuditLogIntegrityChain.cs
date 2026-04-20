using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JAS_MINE_IT15.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogIntegrityChain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Hash",
                table: "AuditLogs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousHash",
                table: "AuditLogs",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Hash",
                table: "AuditLogs",
                column: "Hash",
                unique: true,
                filter: "[Hash] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Hash",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "Hash",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "PreviousHash",
                table: "AuditLogs");
        }
    }
}
