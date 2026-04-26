using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDeemedDisposalToProjections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "apply_deemed_disposal",
                table: "projection_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "deemed_disposal_percent",
                table: "projection_versions",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "apply_deemed_disposal",
                table: "projection_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "apply_deemed_disposal",
                table: "projection_versions");

            migrationBuilder.DropColumn(
                name: "deemed_disposal_percent",
                table: "projection_versions");

            migrationBuilder.DropColumn(
                name: "apply_deemed_disposal",
                table: "projection_settings");
        }
    }
}
