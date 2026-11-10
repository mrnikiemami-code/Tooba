using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Fulfillment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-258: post-dispatch non-delivery / physical return facts and the origin receipt that funds a controlled
/// resend. Additive only: existing parcels keep null/default values and remain readable.
/// </summary>
[DbContext(typeof(FulfillmentDbContext))]
[Migration("20261110120000_AddShipmentNonDeliveryAndReturnReceipt")]
public partial class AddShipmentNonDeliveryAndReturnReceipt : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "non_delivery_reason",
            schema: "fulfillment",
            table: "shipments",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "non_delivery_note",
            schema: "fulfillment",
            table: "shipments",
            type: "character varying(512)",
            maxLength: 512,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "non_delivered_at",
            schema: "fulfillment",
            table: "shipments",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "return_started_at",
            schema: "fulfillment",
            table: "shipments",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "returned_at",
            schema: "fulfillment",
            table: "shipments",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "resend_of_shipment_id",
            schema: "fulfillment",
            table: "shipments",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "ix_shipments_resend_of_shipment_id",
            schema: "fulfillment",
            table: "shipments",
            column: "resend_of_shipment_id",
            filter: "resend_of_shipment_id IS NOT NULL");

        migrationBuilder.CreateTable(
            name: "shipment_return_receipts",
            schema: "fulfillment",
            columns: table => new
            {
                shipment_return_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                fulfillment_id = table.Column<Guid>(type: "uuid", nullable: false),
                received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                received_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_shipment_return_receipts", x => x.shipment_return_receipt_id);
            });

        migrationBuilder.CreateTable(
            name: "shipment_return_receipt_items",
            schema: "fulfillment",
            columns: table => new
            {
                shipment_return_receipt_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                shipment_return_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                condition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_shipment_return_receipt_items", x => x.shipment_return_receipt_item_id);
            });

        migrationBuilder.CreateIndex(
            name: "ux_shipment_return_receipts_shipment",
            schema: "fulfillment",
            table: "shipment_return_receipts",
            column: "shipment_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_shipment_return_receipts_fulfillment_id",
            schema: "fulfillment",
            table: "shipment_return_receipts",
            column: "fulfillment_id");

        migrationBuilder.CreateIndex(
            name: "ix_shipment_return_receipt_items_shipment_return_receipt_id",
            schema: "fulfillment",
            table: "shipment_return_receipt_items",
            column: "shipment_return_receipt_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "shipment_return_receipt_items",
            schema: "fulfillment");

        migrationBuilder.DropTable(
            name: "shipment_return_receipts",
            schema: "fulfillment");

        migrationBuilder.DropIndex(
            name: "ix_shipments_resend_of_shipment_id",
            schema: "fulfillment",
            table: "shipments");

        migrationBuilder.DropColumn(
            name: "resend_of_shipment_id",
            schema: "fulfillment",
            table: "shipments");

        migrationBuilder.DropColumn(
            name: "returned_at",
            schema: "fulfillment",
            table: "shipments");

        migrationBuilder.DropColumn(
            name: "return_started_at",
            schema: "fulfillment",
            table: "shipments");

        migrationBuilder.DropColumn(
            name: "non_delivered_at",
            schema: "fulfillment",
            table: "shipments");

        migrationBuilder.DropColumn(
            name: "non_delivery_note",
            schema: "fulfillment",
            table: "shipments");

        migrationBuilder.DropColumn(
            name: "non_delivery_reason",
            schema: "fulfillment",
            table: "shipments");
    }
}
