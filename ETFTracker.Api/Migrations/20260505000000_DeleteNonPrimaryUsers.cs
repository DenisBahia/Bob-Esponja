using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class DeleteNonPrimaryUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete all users except the primary user.
            // All related data (holdings, transactions, projection_settings, user_settings,
            // projection_versions, user_goals, sell_records, sell_lot_allocations,
            // tax_events, annual_tax_summary, asset_type_deemed_disposal_defaults,
            // profile_shares owned by them) will be removed via CASCADE constraints.
            migrationBuilder.Sql(
                "DELETE FROM users WHERE email != 'denis.bahia.1984@gmail.com';"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This migration is destructive and cannot be reversed.
            // Data deleted by the Up migration cannot be restored automatically.
        }
    }
}

