using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxFreeAllowanceAndIrishFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_irish_investor",
                table: "projection_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_free_allowance_per_year",
                table: "projection_settings",
                type: "numeric(15,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_irish_investor",
                table: "projection_settings");

            migrationBuilder.DropColumn(
                name: "tax_free_allowance_per_year",
                table: "projection_settings");
        }
    }
}
