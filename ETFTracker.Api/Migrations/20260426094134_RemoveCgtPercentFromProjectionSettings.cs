using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCgtPercentFromProjectionSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cgt_percent",
                table: "projection_settings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "cgt_percent",
                table: "projection_settings",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
