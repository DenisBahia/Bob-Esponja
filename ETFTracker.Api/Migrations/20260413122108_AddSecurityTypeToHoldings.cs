using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSecurityTypeToHoldings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "security_type",
                table: "holdings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "security_type",
                table: "holdings");
        }
    }
}
