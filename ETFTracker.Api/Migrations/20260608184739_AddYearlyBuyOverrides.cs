using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddYearlyBuyOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "yearly_buy_overrides_json",
                table: "projection_versions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "yearly_buy_overrides_json",
                table: "projection_settings",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "yearly_buy_overrides_json",
                table: "projection_versions");

            migrationBuilder.DropColumn(
                name: "yearly_buy_overrides_json",
                table: "projection_settings");
        }
    }
}
