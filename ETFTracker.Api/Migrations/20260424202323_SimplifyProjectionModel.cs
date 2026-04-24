using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyProjectionModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "deemed_disposal_percent",
                table: "projection_versions");

            migrationBuilder.DropColumn(
                name: "exclude_pre_existing_from_tax",
                table: "projection_versions");

            migrationBuilder.DropColumn(
                name: "exit_tax_percent",
                table: "projection_versions");

            migrationBuilder.DropColumn(
                name: "is_irish_investor",
                table: "projection_versions");

            migrationBuilder.DropColumn(
                name: "sia_annual_percent",
                table: "projection_versions");

            migrationBuilder.DropColumn(
                name: "tax_free_allowance_per_year",
                table: "projection_versions");

            migrationBuilder.DropColumn(
                name: "exclude_pre_existing_from_tax",
                table: "projection_settings");

            migrationBuilder.AddColumn<decimal>(
                name: "start_amount",
                table: "projection_versions",
                type: "numeric(15,2)",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "tax_free_allowance_per_year",
                table: "projection_settings",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(15,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "exit_tax_percent",
                table: "projection_settings",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 41m,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)");

            migrationBuilder.AlterColumn<bool>(
                name: "deemed_disposal_enabled",
                table: "projection_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "start_amount",
                table: "projection_versions");

            migrationBuilder.AddColumn<decimal>(
                name: "deemed_disposal_percent",
                table: "projection_versions",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 41m);

            migrationBuilder.AddColumn<bool>(
                name: "exclude_pre_existing_from_tax",
                table: "projection_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "exit_tax_percent",
                table: "projection_versions",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "is_irish_investor",
                table: "projection_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "sia_annual_percent",
                table: "projection_versions",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_free_allowance_per_year",
                table: "projection_versions",
                type: "numeric(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "tax_free_allowance_per_year",
                table: "projection_settings",
                type: "numeric(15,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "exit_tax_percent",
                table: "projection_settings",
                type: "numeric(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)",
                oldDefaultValue: 41m);

            migrationBuilder.AlterColumn<bool>(
                name: "deemed_disposal_enabled",
                table: "projection_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "exclude_pre_existing_from_tax",
                table: "projection_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
