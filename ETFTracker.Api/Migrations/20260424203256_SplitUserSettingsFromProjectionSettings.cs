using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class SplitUserSettingsFromProjectionSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create the new user_settings table
            migrationBuilder.CreateTable(
                name: "user_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    is_irish_investor = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    exit_tax_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 41m),
                    deemed_disposal_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 41m),
                    sia_annual_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m),
                    deemed_disposal_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    cgt_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 33m),
                    tax_free_allowance_per_year = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_settings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_settings_user_id",
                table: "user_settings",
                column: "user_id",
                unique: true);

            // 2. Migrate existing tax data from projection_settings → user_settings
            migrationBuilder.Sql(@"
                INSERT INTO user_settings (user_id, is_irish_investor, exit_tax_percent, deemed_disposal_percent,
                    sia_annual_percent, deemed_disposal_enabled, cgt_percent, tax_free_allowance_per_year,
                    created_at, updated_at)
                SELECT user_id,
                       COALESCE(is_irish_investor, false),
                       COALESCE(exit_tax_percent, 41),
                       COALESCE(deemed_disposal_percent, 41),
                       COALESCE(sia_annual_percent, 0),
                       COALESCE(deemed_disposal_enabled, false),
                       COALESCE(cgt_percent, 33),
                       COALESCE(tax_free_allowance_per_year, 0),
                       CURRENT_TIMESTAMP,
                       CURRENT_TIMESTAMP
                FROM projection_settings
                ON CONFLICT (user_id) DO NOTHING;
            ");

            // 3. Drop tax columns from projection_settings
            migrationBuilder.DropColumn(name: "deemed_disposal_enabled", table: "projection_settings");
            migrationBuilder.DropColumn(name: "deemed_disposal_percent", table: "projection_settings");
            migrationBuilder.DropColumn(name: "exit_tax_percent", table: "projection_settings");
            migrationBuilder.DropColumn(name: "is_irish_investor", table: "projection_settings");
            migrationBuilder.DropColumn(name: "sia_annual_percent", table: "projection_settings");
            migrationBuilder.DropColumn(name: "tax_free_allowance_per_year", table: "projection_settings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_settings");

            migrationBuilder.AddColumn<bool>(
                name: "deemed_disposal_enabled",
                table: "projection_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "deemed_disposal_percent",
                table: "projection_settings",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 41m);

            migrationBuilder.AddColumn<decimal>(
                name: "exit_tax_percent",
                table: "projection_settings",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 41m);

            migrationBuilder.AddColumn<bool>(
                name: "is_irish_investor",
                table: "projection_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "sia_annual_percent",
                table: "projection_settings",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_free_allowance_per_year",
                table: "projection_settings",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
