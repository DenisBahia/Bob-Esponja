using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFireSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "fire_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    current_age = table.Column<int>(type: "integer", nullable: true),
                    start_amount = table.Column<decimal>(type: "numeric(15,2)", nullable: true),
                    monthly_investment = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 500m),
                    annual_investment_increase_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 3m),
                    accumulation_return_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 7m),
                    inflation_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 2m),
                    monthly_expenses = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    other_monthly_income = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m),
                    safe_withdrawal_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 4m),
                    withdrawal_return_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 7m),
                    withdrawal_years = table.Column<int>(type: "integer", nullable: false, defaultValue: 30),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fire_settings", x => x.id);
                    table.ForeignKey(
                        name: "FK_fire_settings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_fire_settings_user_id",
                table: "fire_settings",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "fire_settings");
        }
    }
}
