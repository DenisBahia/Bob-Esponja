using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class SetAllHoldingsToEtfWithDeemedDisposal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Set AssetType = 'ETF' for all holdings that don't already have one.
            // Note: the column was created without a HasColumnName mapping so EF Core named
            // it "AssetType" (PascalCase).  PostgreSQL requires double-quotes for mixed-case
            // identifiers.
            migrationBuilder.Sql(
                "UPDATE holdings SET \"AssetType\" = 'ETF' WHERE \"AssetType\" IS NULL OR \"AssetType\" = '';"
            );

            // 2. Mark DeemedDisposalDue = true on every existing transaction.
            migrationBuilder.Sql(
                "UPDATE transactions SET deemed_disposal_due = true;"
            );

            // 3. Upsert asset_type_deemed_disposal_defaults so that every user has
            //    ETF -> deemed_disposal_due = true recorded.  If the row already exists it
            //    is updated; otherwise a new one is inserted.
            migrationBuilder.Sql(@"
                INSERT INTO asset_type_deemed_disposal_defaults (user_id, asset_type, deemed_disposal_due, updated_at)
                SELECT id, 'ETF', true, CURRENT_TIMESTAMP
                FROM users
                ON CONFLICT (user_id, asset_type)
                DO UPDATE SET deemed_disposal_due = true, updated_at = CURRENT_TIMESTAMP;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverting data migrations is inherently lossy because we cannot know which
            // holdings originally had no asset_type and which transactions had
            // deemed_disposal_due = false.  The safest no-op down is documented here.

            // Remove the ETF deemed-disposal default rows that were inserted by this migration
            // (rows that existed before with deemed_disposal_due = false will have been
            // overwritten and cannot be recovered automatically).
            migrationBuilder.Sql(
                "DELETE FROM asset_type_deemed_disposal_defaults WHERE asset_type = 'ETF';"
            );

            // Reset deemed_disposal_due on all transactions back to false.
            migrationBuilder.Sql(
                "UPDATE transactions SET deemed_disposal_due = false;"
            );

            // Clear the ETF asset_type from holdings (set back to null).
            migrationBuilder.Sql(
                "UPDATE holdings SET \"AssetType\" = NULL WHERE \"AssetType\" = 'ETF';"
            );
        }
    }
}

