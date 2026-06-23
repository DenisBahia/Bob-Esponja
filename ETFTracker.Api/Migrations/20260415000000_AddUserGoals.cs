using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserGoals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_goals",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    source_version_id = table.Column<int>(type: "integer", nullable: true),
                    saved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    goal_points_json = table.Column<string>(type: "text", nullable: false, defaultValue: "[]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_goals", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_goals_projection_versions_source_version_id",
                        column: x => x.source_version_id,
                        principalTable: "projection_versions",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_user_goals_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_goals_source_version_id",
                table: "user_goals",
                column: "source_version_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_goals_user_id",
                table: "user_goals",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_goals");
        }
    }
}
