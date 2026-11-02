using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Shipping.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations;

/// <summary>TB-249-R2: durable execution input snapshot before provider create.</summary>
[DbContext(typeof(ShippingDbContext))]
[Migration("20261102220000_CarrierShipmentExecutionSnapshot")]
public partial class CarrierShipmentExecutionSnapshot : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "execution_input_snapshot_json",
            schema: "shipping",
            table: "carrier_shipment_operations",
            type: "text",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "execution_input_snapshot_json",
            schema: "shipping",
            table: "carrier_shipment_operations");
    }
}
