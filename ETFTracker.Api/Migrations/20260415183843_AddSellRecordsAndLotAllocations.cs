using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSellRecordsAndLotAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sell_records",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    holding_id = table.Column<int>(type: "integer", nullable: false),
                    sell_date = table.Column<DateOnly>(type: "date", nullable: false),
                    sell_price = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    total_profit = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    cgt_paid = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    tax_rate_used = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    is_irish_investor = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sell_records", x => x.id);
                    table.ForeignKey(
                        name: "FK_sell_records_holdings_holding_id",
                        column: x => x.holding_id,
                        principalTable: "holdings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sell_lot_allocations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sell_record_id = table.Column<int>(type: "integer", nullable: false),
                    buy_transaction_id = table.Column<int>(type: "integer", nullable: false),
                    quantity_consumed = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    original_cost_per_unit = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    adjusted_cost_per_unit = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    deemed_disposal_date = table.Column<DateOnly>(type: "date", nullable: true),
                    deemed_disposal_price_per_unit = table.Column<decimal>(type: "numeric(12,4)", nullable: true),
                    profit_on_lot = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sell_lot_allocations", x => x.id);
                    table.ForeignKey(
                        name: "FK_sell_lot_allocations_sell_records_sell_record_id",
                        column: x => x.sell_record_id,
                        principalTable: "sell_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sell_lot_allocations_transactions_buy_transaction_id",
                        column: x => x.buy_transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sell_lot_allocations_buy_transaction_id",
                table: "sell_lot_allocations",
                column: "buy_transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_sell_lot_allocations_sell_record_id",
                table: "sell_lot_allocations",
                column: "sell_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_sell_records_holding_id",
                table: "sell_records",
                column: "holding_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sell_lot_allocations");

            migrationBuilder.DropTable(
                name: "sell_records");
        }
    }
}
