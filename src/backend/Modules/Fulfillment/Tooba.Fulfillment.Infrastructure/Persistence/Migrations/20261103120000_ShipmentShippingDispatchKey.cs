using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Fulfillment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations;

/// <summary>TB-249-R5: idempotent Shipping dispatch key on fulfillment shipments.</summary>
[DbContext(typeof(FulfillmentDbContext))]
[Migration("20261103120000_ShipmentShippingDispatchKey")]
public partial class ShipmentShippingDispatchKey : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "shipping_dispatch_key",
            schema: "fulfillment",
            table: "shipments",
            type: "character varying(128)",
            maxLength: 128,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ux_shipments_shipping_dispatch_key",
            schema: "fulfillment",
            table: "shipments",
            column: "shipping_dispatch_key",
            unique: true,
            filter: "shipping_dispatch_key IS NOT NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_shipments_shipping_dispatch_key",
            schema: "fulfillment",
            table: "shipments");

        migrationBuilder.DropColumn(
            name: "shipping_dispatch_key",
            schema: "fulfillment",
            table: "shipments");
    }
}
