using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionNameAndDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_projection_versions_user_id_version_number",
                table: "projection_versions");

            migrationBuilder.DropColumn(
                name: "version_number",
                table: "projection_versions");

            migrationBuilder.AddColumn<string>(
                name: "version_name",
                table: "projection_versions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_default",
                table: "projection_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_projection_versions_user_id",
                table: "projection_versions",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_projection_versions_user_id",
                table: "projection_versions");

            migrationBuilder.DropColumn(
                name: "version_name",
                table: "projection_versions");

            migrationBuilder.DropColumn(
                name: "is_default",
                table: "projection_versions");

            migrationBuilder.AddColumn<int>(
                name: "version_number",
                table: "projection_versions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_projection_versions_user_id_version_number",
                table: "projection_versions",
                columns: new[] { "user_id", "version_number" },
                unique: true);
        }
    }
}

