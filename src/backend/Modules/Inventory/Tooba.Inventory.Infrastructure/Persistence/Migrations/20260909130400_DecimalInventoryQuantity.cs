using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Inventory.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(InventoryDbContext))]
    [Migration("20260909130400_DecimalInventoryQuantity")]
    public partial class DecimalInventoryQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE inventory.stock_positions
                ALTER COLUMN on_hand TYPE numeric(18,6) USING on_hand::numeric(18,6);
                ALTER TABLE inventory.stock_positions
                ALTER COLUMN reserved TYPE numeric(18,6) USING reserved::numeric(18,6);
                ALTER TABLE inventory.reservations
                ALTER COLUMN quantity TYPE numeric(18,6) USING quantity::numeric(18,6);
                ALTER TABLE inventory.return_restock_inbox
                ALTER COLUMN quantity TYPE numeric(18,6) USING quantity::numeric(18,6);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE inventory.stock_positions
                ALTER COLUMN on_hand TYPE integer USING round(on_hand)::integer;
                ALTER TABLE inventory.stock_positions
                ALTER COLUMN reserved TYPE integer USING round(reserved)::integer;
                ALTER TABLE inventory.reservations
                ALTER COLUMN quantity TYPE integer USING round(quantity)::integer;
                ALTER TABLE inventory.return_restock_inbox
                ALTER COLUMN quantity TYPE integer USING round(quantity)::integer;
                """);
        }
    }
}
