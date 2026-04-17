using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTaxEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tax_events",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<int>(type: "integer", nullable: false),
                    holding_id = table.Column<int>(type: "integer", nullable: false),
                    buy_transaction_id = table.Column<int>(type: "integer", nullable: true),
                    sell_record_id = table.Column<int>(type: "integer", nullable: true),
                    event_type = table.Column<string>(type: "text", nullable: false),
                    event_date = table.Column<DateOnly>(type: "date", nullable: false),
                    quantity_at_event = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    cost_basis_per_unit = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    price_per_unit_at_event = table.Column<decimal>(type: "numeric(12,4)", nullable: false),
                    taxable_gain = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    tax_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false),
                    tax_rate_used = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Pending"),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_events", x => x.id);
                    table.ForeignKey(
                        name: "FK_tax_events_holdings_holding_id",
                        column: x => x.holding_id,
                        principalTable: "holdings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tax_events_sell_records_sell_record_id",
                        column: x => x.sell_record_id,
                        principalTable: "sell_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tax_events_transactions_buy_transaction_id",
                        column: x => x.buy_transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_tax_events_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tax_events_buy_transaction_id_event_date",
                table: "tax_events",
                columns: new[] { "buy_transaction_id", "event_date" },
                unique: true,
                filter: "buy_transaction_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_tax_events_holding_id",
                table: "tax_events",
                column: "holding_id");

            migrationBuilder.CreateIndex(
                name: "IX_tax_events_sell_record_id",
                table: "tax_events",
                column: "sell_record_id");

            migrationBuilder.CreateIndex(
                name: "IX_tax_events_user_id",
                table: "tax_events",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tax_events");
        }
    }
}
