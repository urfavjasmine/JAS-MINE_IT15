using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JAS_MINE_IT15.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserLimitColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "DurationMonths",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 12);

            migrationBuilder.AddColumn<int>(
                name: "UserLimit",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 4);

            // ── Users table BarangayId and BarangayName columns ──
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'BarangayId')
                    ALTER TABLE dbo.Users ADD BarangayId INT NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'BarangayName')
                    ALTER TABLE dbo.Users ADD BarangayName NVARCHAR(150) NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserLimit",
                table: "SubscriptionPlans");

            migrationBuilder.AlterColumn<int>(
                name: "DurationMonths",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 12,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);
        }
    }
}
