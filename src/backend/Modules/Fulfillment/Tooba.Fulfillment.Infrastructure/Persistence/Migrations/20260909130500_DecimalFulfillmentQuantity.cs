using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Fulfillment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FulfillmentDbContext))]
    [Migration("20260909130500_DecimalFulfillmentQuantity")]
    public partial class DecimalFulfillmentQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE fulfillment.items
                ALTER COLUMN quantity_ordered TYPE numeric(18,6) USING quantity_ordered::numeric(18,6);
                ALTER TABLE fulfillment.items
                ALTER COLUMN quantity_processing TYPE numeric(18,6) USING quantity_processing::numeric(18,6);
                ALTER TABLE fulfillment.items
                ALTER COLUMN quantity_packed TYPE numeric(18,6) USING quantity_packed::numeric(18,6);
                ALTER TABLE fulfillment.items
                ALTER COLUMN quantity_shipped TYPE numeric(18,6) USING quantity_shipped::numeric(18,6);
                ALTER TABLE fulfillment.shipment_items
                ALTER COLUMN quantity TYPE numeric(18,6) USING quantity::numeric(18,6);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE fulfillment.items
                ALTER COLUMN quantity_ordered TYPE integer USING round(quantity_ordered)::integer;
                ALTER TABLE fulfillment.items
                ALTER COLUMN quantity_processing TYPE integer USING round(quantity_processing)::integer;
                ALTER TABLE fulfillment.items
                ALTER COLUMN quantity_packed TYPE integer USING round(quantity_packed)::integer;
                ALTER TABLE fulfillment.items
                ALTER COLUMN quantity_shipped TYPE integer USING round(quantity_shipped)::integer;
                ALTER TABLE fulfillment.shipment_items
                ALTER COLUMN quantity TYPE integer USING round(quantity)::integer;
                """);
        }
    }
}
