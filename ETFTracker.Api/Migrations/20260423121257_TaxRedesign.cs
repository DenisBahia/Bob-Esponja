using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class TaxRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_profile_shares_users_guest_user_id",
                table: "profile_shares");

            migrationBuilder.DropForeignKey(
                name: "FK_profile_shares_users_owner_id",
                table: "profile_shares");

            migrationBuilder.DropForeignKey(
                name: "FK_sell_lot_allocations_sell_records_sell_record_id",
                table: "sell_lot_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_sell_lot_allocations_transactions_buy_transaction_id",
                table: "sell_lot_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_sell_records_holdings_holding_id",
                table: "sell_records");

            migrationBuilder.DropForeignKey(
                name: "FK_tax_events_holdings_holding_id",
                table: "tax_events");

            migrationBuilder.DropForeignKey(
                name: "FK_tax_events_sell_records_sell_record_id",
                table: "tax_events");

            migrationBuilder.DropForeignKey(
                name: "FK_tax_events_transactions_buy_transaction_id",
                table: "tax_events");

            migrationBuilder.DropForeignKey(
                name: "FK_tax_events_users_user_id",
                table: "tax_events");

            migrationBuilder.DropForeignKey(
                name: "FK_user_goals_users_user_id",
                table: "user_goals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_user_goals",
                table: "user_goals");

            migrationBuilder.DropIndex(
                name: "IX_user_goals_user_id",
                table: "user_goals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tax_events",
                table: "tax_events");

            migrationBuilder.DropIndex(
                name: "IX_tax_events_buy_transaction_id_event_date",
                table: "tax_events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sell_records",
                table: "sell_records");

            migrationBuilder.DropPrimaryKey(
                name: "PK_sell_lot_allocations",
                table: "sell_lot_allocations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_profile_shares",
                table: "profile_shares");

            migrationBuilder.DropIndex(
                name: "IX_profile_shares_owner_id_guest_email",
                table: "profile_shares");

            migrationBuilder.DropColumn(
                name: "cgt_paid",
                table: "sell_records");

            migrationBuilder.DropColumn(
                name: "is_irish_investor",
                table: "sell_records");

            migrationBuilder.RenameTable(
                name: "user_goals",
                newName: "UserGoals");

            migrationBuilder.RenameTable(
                name: "tax_events",
                newName: "TaxEvents");

            migrationBuilder.RenameTable(
                name: "sell_records",
                newName: "SellRecords");

            migrationBuilder.RenameTable(
                name: "sell_lot_allocations",
                newName: "SellLotAllocations");

            migrationBuilder.RenameTable(
                name: "profile_shares",
                newName: "ProfileShares");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "UserGoals",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "UserGoals",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "source_version_id",
                table: "UserGoals",
                newName: "SourceVersionId");

            migrationBuilder.RenameColumn(
                name: "saved_at",
                table: "UserGoals",
                newName: "SavedAt");

            migrationBuilder.RenameColumn(
                name: "goal_points_json",
                table: "UserGoals",
                newName: "GoalPointsJson");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "TaxEvents",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "TaxEvents",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "TaxEvents",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "taxable_gain",
                table: "TaxEvents",
                newName: "TaxableGain");

            migrationBuilder.RenameColumn(
                name: "tax_rate_used",
                table: "TaxEvents",
                newName: "TaxRateUsed");

            migrationBuilder.RenameColumn(
                name: "tax_amount",
                table: "TaxEvents",
                newName: "TaxAmount");

            migrationBuilder.RenameColumn(
                name: "sell_record_id",
                table: "TaxEvents",
                newName: "SellRecordId");

            migrationBuilder.RenameColumn(
                name: "quantity_at_event",
                table: "TaxEvents",
                newName: "QuantityAtEvent");

            migrationBuilder.RenameColumn(
                name: "price_per_unit_at_event",
                table: "TaxEvents",
                newName: "PricePerUnitAtEvent");

            migrationBuilder.RenameColumn(
                name: "paid_at",
                table: "TaxEvents",
                newName: "PaidAt");

            migrationBuilder.RenameColumn(
                name: "holding_id",
                table: "TaxEvents",
                newName: "HoldingId");

            migrationBuilder.RenameColumn(
                name: "event_type",
                table: "TaxEvents",
                newName: "EventType");

            migrationBuilder.RenameColumn(
                name: "event_date",
                table: "TaxEvents",
                newName: "EventDate");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "TaxEvents",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "cost_basis_per_unit",
                table: "TaxEvents",
                newName: "CostBasisPerUnit");

            migrationBuilder.RenameColumn(
                name: "buy_transaction_id",
                table: "TaxEvents",
                newName: "BuyTransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_tax_events_user_id",
                table: "TaxEvents",
                newName: "IX_TaxEvents_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_tax_events_sell_record_id",
                table: "TaxEvents",
                newName: "IX_TaxEvents_SellRecordId");

            migrationBuilder.RenameIndex(
                name: "IX_tax_events_holding_id",
                table: "TaxEvents",
                newName: "IX_TaxEvents_HoldingId");

            migrationBuilder.RenameColumn(
                name: "quantity",
                table: "SellRecords",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "SellRecords",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "total_profit",
                table: "SellRecords",
                newName: "TotalProfit");

            migrationBuilder.RenameColumn(
                name: "tax_rate_used",
                table: "SellRecords",
                newName: "TaxRateUsed");

            migrationBuilder.RenameColumn(
                name: "sell_price",
                table: "SellRecords",
                newName: "SellPrice");

            migrationBuilder.RenameColumn(
                name: "sell_date",
                table: "SellRecords",
                newName: "SellDate");

            migrationBuilder.RenameColumn(
                name: "holding_id",
                table: "SellRecords",
                newName: "HoldingId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "SellRecords",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_sell_records_holding_id",
                table: "SellRecords",
                newName: "IX_SellRecords_HoldingId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "SellLotAllocations",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "sell_record_id",
                table: "SellLotAllocations",
                newName: "SellRecordId");

            migrationBuilder.RenameColumn(
                name: "quantity_consumed",
                table: "SellLotAllocations",
                newName: "QuantityConsumed");

            migrationBuilder.RenameColumn(
                name: "profit_on_lot",
                table: "SellLotAllocations",
                newName: "ProfitOnLot");

            migrationBuilder.RenameColumn(
                name: "original_cost_per_unit",
                table: "SellLotAllocations",
                newName: "OriginalCostPerUnit");

            migrationBuilder.RenameColumn(
                name: "deemed_disposal_price_per_unit",
                table: "SellLotAllocations",
                newName: "DeemedDisposalPricePerUnit");

            migrationBuilder.RenameColumn(
                name: "deemed_disposal_date",
                table: "SellLotAllocations",
                newName: "DeemedDisposalDate");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "SellLotAllocations",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "buy_transaction_id",
                table: "SellLotAllocations",
                newName: "BuyTransactionId");

            migrationBuilder.RenameColumn(
                name: "adjusted_cost_per_unit",
                table: "SellLotAllocations",
                newName: "AdjustedCostPerUnit");

            migrationBuilder.RenameIndex(
                name: "IX_sell_lot_allocations_sell_record_id",
                table: "SellLotAllocations",
                newName: "IX_SellLotAllocations_SellRecordId");

            migrationBuilder.RenameIndex(
                name: "IX_sell_lot_allocations_buy_transaction_id",
                table: "SellLotAllocations",
                newName: "IX_SellLotAllocations_BuyTransactionId");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "ProfileShares",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ProfileShares",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "ProfileShares",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "owner_id",
                table: "ProfileShares",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "is_read_only",
                table: "ProfileShares",
                newName: "IsReadOnly");

            migrationBuilder.RenameColumn(
                name: "guest_user_id",
                table: "ProfileShares",
                newName: "GuestUserId");

            migrationBuilder.RenameColumn(
                name: "guest_email",
                table: "ProfileShares",
                newName: "GuestEmail");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "ProfileShares",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_profile_shares_guest_user_id",
                table: "ProfileShares",
                newName: "IX_ProfileShares_GuestUserId");

            migrationBuilder.AddColumn<bool>(
                name: "deemed_disposal_due",
                table: "transactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "deemed_disposal_percent",
                table: "projection_versions",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 41m);

            migrationBuilder.AddColumn<bool>(
                name: "is_irish_investor",
                table: "projection_versions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_free_allowance_per_year",
                table: "projection_versions",
                type: "numeric(15,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "deemed_disposal_enabled",
                table: "projection_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // PostgreSQL cannot cast text→integer automatically; use USING expression.
            migrationBuilder.Sql(
                """
                ALTER TABLE "TaxEvents"
                    ALTER COLUMN "Status" DROP DEFAULT,
                    ALTER COLUMN "Status" TYPE integer
                        USING (CASE WHEN "Status" = 'Paid' THEN 1 ELSE 0 END);
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxableGain",
                table: "TaxEvents",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxRateUsed",
                table: "TaxEvents",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxAmount",
                table: "TaxEvents",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityAtEvent",
                table: "TaxEvents",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PricePerUnitAtEvent",
                table: "TaxEvents",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)");

            // PostgreSQL cannot cast text→integer automatically; use USING expression.
            migrationBuilder.Sql(
                """
                ALTER TABLE "TaxEvents"
                    ALTER COLUMN "EventType" TYPE integer
                        USING (CASE WHEN "EventType" = 'DeemedDisposal' THEN 1 ELSE 0 END);
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "TaxEvents",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<decimal>(
                name: "CostBasisPerUnit",
                table: "TaxEvents",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)");

            migrationBuilder.AddColumn<string>(
                name: "TaxSubType",
                table: "TaxEvents",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "SellRecords",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalProfit",
                table: "SellRecords",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TaxRateUsed",
                table: "SellRecords",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(5,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "SellPrice",
                table: "SellRecords",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "SellRecords",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmountSaved",
                table: "SellRecords",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "TaxType",
                table: "SellRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityConsumed",
                table: "SellLotAllocations",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ProfitOnLot",
                table: "SellLotAllocations",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "OriginalCostPerUnit",
                table: "SellLotAllocations",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)");

            migrationBuilder.AlterColumn<decimal>(
                name: "DeemedDisposalPricePerUnit",
                table: "SellLotAllocations",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "SellLotAllocations",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<decimal>(
                name: "AdjustedCostPerUnit",
                table: "SellLotAllocations",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,4)");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "ProfileShares",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "ProfileShares",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AlterColumn<bool>(
                name: "IsReadOnly",
                table: "ProfileShares",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "GuestEmail",
                table: "ProfileShares",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ProfileShares",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserGoals",
                table: "UserGoals",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaxEvents",
                table: "TaxEvents",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SellRecords",
                table: "SellRecords",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SellLotAllocations",
                table: "SellLotAllocations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProfileShares",
                table: "ProfileShares",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "annual_tax_summary",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    tax_year = table.Column<int>(type: "integer", nullable: false),
                    tax_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    holding_id = table.Column<int>(type: "integer", nullable: true),
                    total_profits = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    total_losses = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    net_gain = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    allowance_applied = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    deemed_disposal_credit = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    taxable_gain = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    tax_due = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    tax_rate_used = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    recalculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_annual_tax_summary", x => x.id);
                    table.ForeignKey(
                        name: "FK_annual_tax_summary_holdings_holding_id",
                        column: x => x.holding_id,
                        principalTable: "holdings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_annual_tax_summary_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asset_type_deemed_disposal_defaults",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    asset_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    deemed_disposal_due = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_type_deemed_disposal_defaults", x => x.id);
                    table.ForeignKey(
                        name: "FK_asset_type_deemed_disposal_defaults_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserGoals_UserId",
                table: "UserGoals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TaxEvents_BuyTransactionId",
                table: "TaxEvents",
                column: "BuyTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileShares_OwnerId",
                table: "ProfileShares",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_annual_tax_summary_holding_id",
                table: "annual_tax_summary",
                column: "holding_id");

            migrationBuilder.CreateIndex(
                name: "IX_annual_tax_summary_user_id",
                table: "annual_tax_summary",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_asset_type_deemed_disposal_defaults_user_id_asset_type",
                table: "asset_type_deemed_disposal_defaults",
                columns: new[] { "user_id", "asset_type" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileShares_users_GuestUserId",
                table: "ProfileShares",
                column: "GuestUserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileShares_users_OwnerId",
                table: "ProfileShares",
                column: "OwnerId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SellLotAllocations_SellRecords_SellRecordId",
                table: "SellLotAllocations",
                column: "SellRecordId",
                principalTable: "SellRecords",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SellLotAllocations_transactions_BuyTransactionId",
                table: "SellLotAllocations",
                column: "BuyTransactionId",
                principalTable: "transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SellRecords_holdings_HoldingId",
                table: "SellRecords",
                column: "HoldingId",
                principalTable: "holdings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxEvents_SellRecords_SellRecordId",
                table: "TaxEvents",
                column: "SellRecordId",
                principalTable: "SellRecords",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaxEvents_holdings_HoldingId",
                table: "TaxEvents",
                column: "HoldingId",
                principalTable: "holdings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxEvents_transactions_BuyTransactionId",
                table: "TaxEvents",
                column: "BuyTransactionId",
                principalTable: "transactions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaxEvents_users_UserId",
                table: "TaxEvents",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserGoals_users_UserId",
                table: "UserGoals",
                column: "UserId",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfileShares_users_GuestUserId",
                table: "ProfileShares");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfileShares_users_OwnerId",
                table: "ProfileShares");

            migrationBuilder.DropForeignKey(
                name: "FK_SellLotAllocations_SellRecords_SellRecordId",
                table: "SellLotAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_SellLotAllocations_transactions_BuyTransactionId",
                table: "SellLotAllocations");

            migrationBuilder.DropForeignKey(
                name: "FK_SellRecords_holdings_HoldingId",
                table: "SellRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxEvents_SellRecords_SellRecordId",
                table: "TaxEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxEvents_holdings_HoldingId",
                table: "TaxEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxEvents_transactions_BuyTransactionId",
                table: "TaxEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxEvents_users_UserId",
                table: "TaxEvents");

            migrationBuilder.DropForeignKey(
                name: "FK_UserGoals_users_UserId",
                table: "UserGoals");

            migrationBuilder.DropTable(
                name: "annual_tax_summary");

            migrationBuilder.DropTable(
                name: "asset_type_deemed_disposal_defaults");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserGoals",
                table: "UserGoals");

            migrationBuilder.DropIndex(
                name: "IX_UserGoals_UserId",
                table: "UserGoals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaxEvents",
                table: "TaxEvents");

            migrationBuilder.DropIndex(
                name: "IX_TaxEvents_BuyTransactionId",
                table: "TaxEvents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SellRecords",
                table: "SellRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SellLotAllocations",
                table: "SellLotAllocations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProfileShares",
                table: "ProfileShares");

            migrationBuilder.DropIndex(
                name: "IX_ProfileShares_OwnerId",
                table: "ProfileShares");

            migrationBuilder.DropColumn(
                name: "deemed_disposal_due",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "deemed_disposal_percent",
                table: "projection_versions");

            migrationBuilder.DropColumn(
                name: "is_irish_investor",
                table: "projection_versions");

            migrationBuilder.DropColumn(
                name: "tax_free_allowance_per_year",
                table: "projection_versions");

            migrationBuilder.DropColumn(
                name: "deemed_disposal_enabled",
                table: "projection_settings");

            migrationBuilder.DropColumn(
                name: "TaxSubType",
                table: "TaxEvents");

            migrationBuilder.DropColumn(
                name: "TaxAmountSaved",
                table: "SellRecords");

            migrationBuilder.DropColumn(
                name: "TaxType",
                table: "SellRecords");

            migrationBuilder.RenameTable(
                name: "UserGoals",
                newName: "user_goals");

            migrationBuilder.RenameTable(
                name: "TaxEvents",
                newName: "tax_events");

            migrationBuilder.RenameTable(
                name: "SellRecords",
                newName: "sell_records");

            migrationBuilder.RenameTable(
                name: "SellLotAllocations",
                newName: "sell_lot_allocations");

            migrationBuilder.RenameTable(
                name: "ProfileShares",
                newName: "profile_shares");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "user_goals",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "user_goals",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "SourceVersionId",
                table: "user_goals",
                newName: "source_version_id");

            migrationBuilder.RenameColumn(
                name: "SavedAt",
                table: "user_goals",
                newName: "saved_at");

            migrationBuilder.RenameColumn(
                name: "GoalPointsJson",
                table: "user_goals",
                newName: "goal_points_json");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "tax_events",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "tax_events",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "tax_events",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "TaxableGain",
                table: "tax_events",
                newName: "taxable_gain");

            migrationBuilder.RenameColumn(
                name: "TaxRateUsed",
                table: "tax_events",
                newName: "tax_rate_used");

            migrationBuilder.RenameColumn(
                name: "TaxAmount",
                table: "tax_events",
                newName: "tax_amount");

            migrationBuilder.RenameColumn(
                name: "SellRecordId",
                table: "tax_events",
                newName: "sell_record_id");

            migrationBuilder.RenameColumn(
                name: "QuantityAtEvent",
                table: "tax_events",
                newName: "quantity_at_event");

            migrationBuilder.RenameColumn(
                name: "PricePerUnitAtEvent",
                table: "tax_events",
                newName: "price_per_unit_at_event");

            migrationBuilder.RenameColumn(
                name: "PaidAt",
                table: "tax_events",
                newName: "paid_at");

            migrationBuilder.RenameColumn(
                name: "HoldingId",
                table: "tax_events",
                newName: "holding_id");

            migrationBuilder.RenameColumn(
                name: "EventType",
                table: "tax_events",
                newName: "event_type");

            migrationBuilder.RenameColumn(
                name: "EventDate",
                table: "tax_events",
                newName: "event_date");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "tax_events",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "CostBasisPerUnit",
                table: "tax_events",
                newName: "cost_basis_per_unit");

            migrationBuilder.RenameColumn(
                name: "BuyTransactionId",
                table: "tax_events",
                newName: "buy_transaction_id");

            migrationBuilder.RenameIndex(
                name: "IX_TaxEvents_UserId",
                table: "tax_events",
                newName: "IX_tax_events_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_TaxEvents_SellRecordId",
                table: "tax_events",
                newName: "IX_tax_events_sell_record_id");

            migrationBuilder.RenameIndex(
                name: "IX_TaxEvents_HoldingId",
                table: "tax_events",
                newName: "IX_tax_events_holding_id");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "sell_records",
                newName: "quantity");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "sell_records",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "TotalProfit",
                table: "sell_records",
                newName: "total_profit");

            migrationBuilder.RenameColumn(
                name: "TaxRateUsed",
                table: "sell_records",
                newName: "tax_rate_used");

            migrationBuilder.RenameColumn(
                name: "SellPrice",
                table: "sell_records",
                newName: "sell_price");

            migrationBuilder.RenameColumn(
                name: "SellDate",
                table: "sell_records",
                newName: "sell_date");

            migrationBuilder.RenameColumn(
                name: "HoldingId",
                table: "sell_records",
                newName: "holding_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "sell_records",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_SellRecords_HoldingId",
                table: "sell_records",
                newName: "IX_sell_records_holding_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "sell_lot_allocations",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SellRecordId",
                table: "sell_lot_allocations",
                newName: "sell_record_id");

            migrationBuilder.RenameColumn(
                name: "QuantityConsumed",
                table: "sell_lot_allocations",
                newName: "quantity_consumed");

            migrationBuilder.RenameColumn(
                name: "ProfitOnLot",
                table: "sell_lot_allocations",
                newName: "profit_on_lot");

            migrationBuilder.RenameColumn(
                name: "OriginalCostPerUnit",
                table: "sell_lot_allocations",
                newName: "original_cost_per_unit");

            migrationBuilder.RenameColumn(
                name: "DeemedDisposalPricePerUnit",
                table: "sell_lot_allocations",
                newName: "deemed_disposal_price_per_unit");

            migrationBuilder.RenameColumn(
                name: "DeemedDisposalDate",
                table: "sell_lot_allocations",
                newName: "deemed_disposal_date");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "sell_lot_allocations",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "BuyTransactionId",
                table: "sell_lot_allocations",
                newName: "buy_transaction_id");

            migrationBuilder.RenameColumn(
                name: "AdjustedCostPerUnit",
                table: "sell_lot_allocations",
                newName: "adjusted_cost_per_unit");

            migrationBuilder.RenameIndex(
                name: "IX_SellLotAllocations_SellRecordId",
                table: "sell_lot_allocations",
                newName: "IX_sell_lot_allocations_sell_record_id");

            migrationBuilder.RenameIndex(
                name: "IX_SellLotAllocations_BuyTransactionId",
                table: "sell_lot_allocations",
                newName: "IX_sell_lot_allocations_buy_transaction_id");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "profile_shares",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "profile_shares",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "profile_shares",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "profile_shares",
                newName: "owner_id");

            migrationBuilder.RenameColumn(
                name: "IsReadOnly",
                table: "profile_shares",
                newName: "is_read_only");

            migrationBuilder.RenameColumn(
                name: "GuestUserId",
                table: "profile_shares",
                newName: "guest_user_id");

            migrationBuilder.RenameColumn(
                name: "GuestEmail",
                table: "profile_shares",
                newName: "guest_email");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "profile_shares",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_ProfileShares_GuestUserId",
                table: "profile_shares",
                newName: "IX_profile_shares_guest_user_id");

            migrationBuilder.Sql(
                """
                ALTER TABLE "tax_events"
                    ALTER COLUMN "status" TYPE text
                        USING (CASE WHEN "status" = 1 THEN 'Paid' ELSE 'Pending' END),
                    ALTER COLUMN "status" SET DEFAULT 'Pending';
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "taxable_gain",
                table: "tax_events",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "tax_rate_used",
                table: "tax_events",
                type: "numeric(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "tax_amount",
                table: "tax_events",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity_at_event",
                table: "tax_events",
                type: "numeric(12,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "price_per_unit_at_event",
                table: "tax_events",
                type: "numeric(12,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.Sql(
                """
                ALTER TABLE "tax_events"
                    ALTER COLUMN "event_type" TYPE text
                        USING (CASE WHEN "event_type" = 1 THEN 'DeemedDisposal' ELSE 'Sell' END);
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "tax_events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "cost_basis_per_unit",
                table: "tax_events",
                type: "numeric(12,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "sell_records",
                type: "numeric(12,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "total_profit",
                table: "sell_records",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "tax_rate_used",
                table: "sell_records",
                type: "numeric(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "sell_price",
                table: "sell_records",
                type: "numeric(12,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "sell_records",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<decimal>(
                name: "cgt_paid",
                table: "sell_records",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "is_irish_investor",
                table: "sell_records",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity_consumed",
                table: "sell_lot_allocations",
                type: "numeric(12,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "profit_on_lot",
                table: "sell_lot_allocations",
                type: "numeric(12,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "original_cost_per_unit",
                table: "sell_lot_allocations",
                type: "numeric(12,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<decimal>(
                name: "deemed_disposal_price_per_unit",
                table: "sell_lot_allocations",
                type: "numeric(12,4)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "sell_lot_allocations",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<decimal>(
                name: "adjusted_cost_per_unit",
                table: "sell_lot_allocations",
                type: "numeric(12,4)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AlterColumn<int>(
                name: "status",
                table: "profile_shares",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "profile_shares",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<bool>(
                name: "is_read_only",
                table: "profile_shares",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "guest_email",
                table: "profile_shares",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "profile_shares",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddPrimaryKey(
                name: "PK_user_goals",
                table: "user_goals",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tax_events",
                table: "tax_events",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sell_records",
                table: "sell_records",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_sell_lot_allocations",
                table: "sell_lot_allocations",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_profile_shares",
                table: "profile_shares",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_user_goals_user_id",
                table: "user_goals",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tax_events_buy_transaction_id_event_date",
                table: "tax_events",
                columns: new[] { "buy_transaction_id", "event_date" },
                unique: true,
                filter: "buy_transaction_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_profile_shares_owner_id_guest_email",
                table: "profile_shares",
                columns: new[] { "owner_id", "guest_email" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_profile_shares_users_guest_user_id",
                table: "profile_shares",
                column: "guest_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_profile_shares_users_owner_id",
                table: "profile_shares",
                column: "owner_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sell_lot_allocations_sell_records_sell_record_id",
                table: "sell_lot_allocations",
                column: "sell_record_id",
                principalTable: "sell_records",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_sell_lot_allocations_transactions_buy_transaction_id",
                table: "sell_lot_allocations",
                column: "buy_transaction_id",
                principalTable: "transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_sell_records_holdings_holding_id",
                table: "sell_records",
                column: "holding_id",
                principalTable: "holdings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tax_events_holdings_holding_id",
                table: "tax_events",
                column: "holding_id",
                principalTable: "holdings",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tax_events_sell_records_sell_record_id",
                table: "tax_events",
                column: "sell_record_id",
                principalTable: "sell_records",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_tax_events_transactions_buy_transaction_id",
                table: "tax_events",
                column: "buy_transaction_id",
                principalTable: "transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_tax_events_users_user_id",
                table: "tax_events",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_goals_users_user_id",
                table: "user_goals",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
