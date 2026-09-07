using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Fulfillment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FulfillmentDbContext))]
    [Migration("20260907090000_AddFulfillmentItemQuantityPacked")]
    public partial class AddFulfillmentItemQuantityPacked : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "quantity_packed",
                schema: "fulfillment",
                table: "items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill: units already past packing should expose packed qty for allocation math.
            migrationBuilder.Sql("""
                UPDATE fulfillment.items AS i
                SET quantity_packed = i.quantity_ordered
                FROM fulfillment.fulfillments AS f
                WHERE i.fulfillment_id = f.fulfillment_id
                  AND f.status IN ('Packed', 'Dispatched', 'InTransit', 'Delivered');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "quantity_packed",
                schema: "fulfillment",
                table: "items");
        }
    }
}
