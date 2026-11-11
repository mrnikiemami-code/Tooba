using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-262: durable damaged-goods decisions and the linked replacement parcel's own reservation. Additive only:
/// historical parcels keep NULL, every existing receipt/reservation/parcel row stays untouched, and damaged units are
/// never converted into saleable stock or into resend capacity by this migration.
/// </summary>
public partial class AddDamagedGoodsResolution : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "damaged_resolution_id",
            schema: "fulfillment",
            table: "shipments",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "replacement_reservation_id",
            schema: "fulfillment",
            table: "shipments",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "damaged_goods_resolutions",
            schema: "fulfillment",
            columns: table => new
            {
                damaged_goods_resolution_id = table.Column<Guid>(type: "uuid", nullable: false),
                fulfillment_id = table.Column<Guid>(type: "uuid", nullable: false),
                shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                shipment_return_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                replacement_reservation_id = table.Column<Guid>(type: "uuid", nullable: true),
                replacement_shipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                occurrence_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                attempt = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_damaged_goods_resolutions", x => x.damaged_goods_resolution_id);
            });

        migrationBuilder.CreateIndex(
            name: "ix_shipments_damaged_resolution_id",
            schema: "fulfillment",
            table: "shipments",
            column: "damaged_resolution_id",
            filter: "damaged_resolution_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_damaged_goods_resolutions_fulfillment_id",
            schema: "fulfillment",
            table: "damaged_goods_resolutions",
            column: "fulfillment_id");

        migrationBuilder.CreateIndex(
            name: "ix_damaged_goods_resolutions_shipment_id",
            schema: "fulfillment",
            table: "damaged_goods_resolutions",
            column: "shipment_id");

        migrationBuilder.CreateIndex(
            name: "ix_damaged_goods_resolutions_replacement_reservation_id",
            schema: "fulfillment",
            table: "damaged_goods_resolutions",
            column: "replacement_reservation_id",
            filter: "replacement_reservation_id IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "ix_damaged_goods_resolutions_replacement_shipment_id",
            schema: "fulfillment",
            table: "damaged_goods_resolutions",
            column: "replacement_shipment_id",
            filter: "replacement_shipment_id IS NOT NULL");

        // TB-262: one durable decision per reviewed occurrence inside its owning unit, so a retried reviewed intent
        // can only ever answer the recorded decision — never record a second one for the same damaged units.
        migrationBuilder.CreateIndex(
            name: "ux_damaged_goods_resolutions_fulfillment_occurrence",
            schema: "fulfillment",
            table: "damaged_goods_resolutions",
            columns: new[] { "fulfillment_id", "occurrence_key" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "damaged_goods_resolutions",
            schema: "fulfillment");

        migrationBuilder.DropIndex(
            name: "ix_shipments_damaged_resolution_id",
            schema: "fulfillment",
            table: "shipments");

        migrationBuilder.DropColumn(
            name: "damaged_resolution_id",
            schema: "fulfillment",
            table: "shipments");

        migrationBuilder.DropColumn(
            name: "replacement_reservation_id",
            schema: "fulfillment",
            table: "shipments");
    }
}
