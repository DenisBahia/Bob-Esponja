using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceSourceColumnToHoldings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"ALTER TABLE holdings
ADD COLUMN IF NOT EXISTS price_source character varying(50);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: this migration is a duplicate of an earlier migration that already
            // introduced holdings.price_source, so rolling back only this later migration
            // must not remove the column.
        }
    }
}
