using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Fulfillment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-258-R1: durable operator identity for the post-dispatch facts and the resend occurrence. Additive only:
/// historical parcels keep NULL and stay readable; no receipt or accepted history is rewritten.
/// </summary>
[DbContext(typeof(FulfillmentDbContext))]
[Migration("20261110160000_AddShipmentReturnOperatorAudit")]
public partial class AddShipmentReturnOperatorAudit : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "created_by_user_id",
            schema: "fulfillment",
            table: "shipments",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "non_delivered_by_user_id",
            schema: "fulfillment",
            table: "shipments",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "return_started_by_user_id",
            schema: "fulfillment",
            table: "shipments",
            type: "uuid",
            nullable: true);

        // TB-258-R1: durable exactly-once for a physical receipt line per condition, so a parcel line
        // split as usable+damaged is representable while a duplicate (line, condition) pair is refused by
        // the database, not only by the aggregate.
        migrationBuilder.CreateIndex(
            name: "ux_shipment_return_receipt_items_line_condition",
            schema: "fulfillment",
            table: "shipment_return_receipt_items",
            columns: new[] { "shipment_return_receipt_id", "order_line_id", "condition" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_shipment_return_receipt_items_line_condition",
            schema: "fulfillment",
            table: "shipment_return_receipt_items");

        migrationBuilder.DropColumn(
            name: "return_started_by_user_id",
            schema: "fulfillment",
            table: "shipments");

        migrationBuilder.DropColumn(
            name: "non_delivered_by_user_id",
            schema: "fulfillment",
            table: "shipments");

        migrationBuilder.DropColumn(
            name: "created_by_user_id",
            schema: "fulfillment",
            table: "shipments");
    }
}
