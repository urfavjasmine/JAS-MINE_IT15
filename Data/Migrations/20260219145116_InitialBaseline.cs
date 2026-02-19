using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JAS_MINE_IT15.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Baseline migration - tables already exist from JAS_MINE_DB_Schema.sql
    /// This migration is intentionally empty.
    /// </summary>
    public partial class InitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty - tables already exist in database
            // Created via JAS_MINE_DB_Schema.sql
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty - do not drop existing tables
        }
    }
}
