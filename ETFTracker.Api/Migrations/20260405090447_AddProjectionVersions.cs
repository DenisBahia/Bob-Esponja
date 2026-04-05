using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectionVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "projection_versions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    yearly_return_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    monthly_buy_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    annual_buy_increase_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    projection_years = table.Column<int>(type: "integer", nullable: false),
                    inflation_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    cgt_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    exit_tax_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    exclude_pre_existing_from_tax = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    data_points_json = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projection_versions", x => x.id);
                    table.ForeignKey(
                        name: "FK_projection_versions_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_projection_versions_user_id_version_number",
                table: "projection_versions",
                columns: new[] { "user_id", "version_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "projection_versions");
        }
    }
}
