using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JAS_MINE_IT15.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditHashAlgorithmMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HashAlgorithm",
                table: "AuditLogs",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HashAlgorithm",
                table: "AuditLogs");
        }
    }
}
