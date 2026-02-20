using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JAS_MINE_IT15.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsArchivedField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SharedDocuments");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Policies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ActionTaken",
                table: "LessonsLearned",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateRecorded",
                table: "LessonsLearned",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "LessonsLearned",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Problem",
                table: "LessonsLearned",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Recommendation",
                table: "LessonsLearned",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Result",
                table: "LessonsLearned",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "KnowledgeRepository",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "KnowledgeDiscussions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "BestPractices",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OwnerOffice",
                table: "BestPractices",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Purpose",
                table: "BestPractices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResourcesNeeded",
                table: "BestPractices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "BestPractices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Steps",
                table: "BestPractices",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "Announcements",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Policies");

            migrationBuilder.DropColumn(
                name: "ActionTaken",
                table: "LessonsLearned");

            migrationBuilder.DropColumn(
                name: "DateRecorded",
                table: "LessonsLearned");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "LessonsLearned");

            migrationBuilder.DropColumn(
                name: "Problem",
                table: "LessonsLearned");

            migrationBuilder.DropColumn(
                name: "Recommendation",
                table: "LessonsLearned");

            migrationBuilder.DropColumn(
                name: "Result",
                table: "LessonsLearned");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "KnowledgeRepository");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "KnowledgeDiscussions");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "BestPractices");

            migrationBuilder.DropColumn(
                name: "OwnerOffice",
                table: "BestPractices");

            migrationBuilder.DropColumn(
                name: "Purpose",
                table: "BestPractices");

            migrationBuilder.DropColumn(
                name: "ResourcesNeeded",
                table: "BestPractices");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "BestPractices");

            migrationBuilder.DropColumn(
                name: "Steps",
                table: "BestPractices");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "Announcements");

            migrationBuilder.CreateTable(
                name: "SharedDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SharedById = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    DownloadCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    FileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SharedDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SharedDocuments_Users_SharedById",
                        column: x => x.SharedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SharedDocuments_SharedById",
                table: "SharedDocuments",
                column: "SharedById");
        }
    }
}
