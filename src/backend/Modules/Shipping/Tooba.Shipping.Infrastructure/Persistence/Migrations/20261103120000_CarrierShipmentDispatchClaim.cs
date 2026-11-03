using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Shipping.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations;

/// <summary>TB-249-R5: exclusive dispatch claim + external-attempted flag before provider HTTP.</summary>
[DbContext(typeof(ShippingDbContext))]
[Migration("20261103120000_CarrierShipmentDispatchClaim")]
public partial class CarrierShipmentDispatchClaim : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "dispatch_claim_token",
            schema: "shipping",
            table: "carrier_shipment_operations",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "dispatch_claim_expires_at",
            schema: "shipping",
            table: "carrier_shipment_operations",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "external_dispatch_attempted",
            schema: "shipping",
            table: "carrier_shipment_operations",
            type: "boolean",
            nullable: false,
            defaultValue: false);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "dispatch_claim_token",
            schema: "shipping",
            table: "carrier_shipment_operations");

        migrationBuilder.DropColumn(
            name: "dispatch_claim_expires_at",
            schema: "shipping",
            table: "carrier_shipment_operations");

        migrationBuilder.DropColumn(
            name: "external_dispatch_attempted",
            schema: "shipping",
            table: "carrier_shipment_operations");
    }
}
