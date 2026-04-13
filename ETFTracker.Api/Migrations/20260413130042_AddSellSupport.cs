using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ETFTracker.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSellSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "transaction_type",
                table: "transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "asset_tax_rates",
                columns: table => new
                {
                    security_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    exit_tax_percent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asset_tax_rates", x => x.security_type);
                });

            migrationBuilder.CreateTable(
                name: "sell_allocations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sell_transaction_id = table.Column<int>(type: "integer", nullable: false),
                    buy_transaction_id = table.Column<int>(type: "integer", nullable: false),
                    allocated_quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    buy_price = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sell_allocations", x => x.id);
                    table.ForeignKey(
                        name: "FK_sell_allocations_transactions_buy_transaction_id",
                        column: x => x.buy_transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sell_allocations_transactions_sell_transaction_id",
                        column: x => x.sell_transaction_id,
                        principalTable: "transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sell_allocations_buy_transaction_id",
                table: "sell_allocations",
                column: "buy_transaction_id");

            migrationBuilder.CreateIndex(
                name: "IX_sell_allocations_sell_transaction_id",
                table: "sell_allocations",
                column: "sell_transaction_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_tax_rates");

            migrationBuilder.DropTable(
                name: "sell_allocations");

            migrationBuilder.DropColumn(
                name: "transaction_type",
                table: "transactions");
        }
    }
}
