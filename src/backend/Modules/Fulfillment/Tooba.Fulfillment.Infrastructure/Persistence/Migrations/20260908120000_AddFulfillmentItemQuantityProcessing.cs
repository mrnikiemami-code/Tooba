using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Fulfillment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FulfillmentDbContext))]
    [Migration("20260908120000_AddFulfillmentItemQuantityProcessing")]
    public partial class AddFulfillmentItemQuantityProcessing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "quantity_processing",
                schema: "fulfillment",
                table: "items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Units already past start-processing were whole-unit; keep remaining packable.
            migrationBuilder.Sql("""
                UPDATE fulfillment.items AS i
                SET quantity_processing = i.quantity_ordered
                FROM fulfillment.fulfillments AS f
                WHERE i.fulfillment_id = f.fulfillment_id
                  AND f.status IN ('Processing', 'Packed', 'Dispatched', 'InTransit', 'Delivered');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "quantity_processing",
                schema: "fulfillment",
                table: "items");
        }
    }
}
