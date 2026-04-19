using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDeemedDisposalPercent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "deemed_disposal_percent",
                table: "projection_settings",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 41m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deemed_disposal_percent",
                table: "projection_settings");
        }
    }
}
