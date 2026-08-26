using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialFulfillment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "fulfillment");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "fulfillment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    event_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    deployment_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    edition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dead_lettered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("pk_outbox_messages", x => x.id));

            migrationBuilder.CreateTable(
                name: "payment_inbox",
                schema: "fulfillment",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_payment_inbox", x => x.event_id));

            migrationBuilder.CreateTable(
                name: "fulfillments",
                schema: "fulfillment",
                columns: table => new
                {
                    fulfillment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checkout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    placed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    contact_mobile = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    province_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    city_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    postal_address = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    shipping_method_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    shipping_method_label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_fulfillments", x => x.fulfillment_id));

            migrationBuilder.CreateIndex(
                name: "ix_fulfillments_seller_order_id",
                schema: "fulfillment",
                table: "fulfillments",
                column: "seller_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fulfillments_seller_party_id",
                schema: "fulfillment",
                table: "fulfillments",
                column: "seller_party_id");

            migrationBuilder.CreateIndex(
                name: "ix_fulfillments_checkout_id",
                schema: "fulfillment",
                table: "fulfillments",
                column: "checkout_id");

            migrationBuilder.CreateTable(
                name: "items",
                schema: "fulfillment",
                columns: table => new
                {
                    fulfillment_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fulfillment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_ordered = table.Column<int>(type: "integer", nullable: false),
                    quantity_shipped = table.Column<int>(type: "integer", nullable: false),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reservation_consumed = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_items", x => x.fulfillment_item_id));

            migrationBuilder.CreateIndex(
                name: "ix_items_fulfillment_id",
                schema: "fulfillment",
                table: "items",
                column: "fulfillment_id");

            migrationBuilder.CreateTable(
                name: "shipments",
                schema: "fulfillment",
                columns: table => new
                {
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fulfillment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    carrier_display_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    tracking_reference = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    dispatched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_shipments", x => x.shipment_id));

            migrationBuilder.CreateIndex(
                name: "ix_shipments_fulfillment_id",
                schema: "fulfillment",
                table: "shipments",
                column: "fulfillment_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_tracking_reference",
                schema: "fulfillment",
                table: "shipments",
                column: "tracking_reference",
                unique: true,
                filter: "tracking_reference IS NOT NULL");

            migrationBuilder.CreateTable(
                name: "shipment_items",
                schema: "fulfillment",
                columns: table => new
                {
                    shipment_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_shipment_items", x => x.shipment_item_id));

            migrationBuilder.CreateIndex(
                name: "ix_shipment_items_shipment_id",
                schema: "fulfillment",
                table: "shipment_items",
                column: "shipment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "shipment_items", schema: "fulfillment");
            migrationBuilder.DropTable(name: "shipments", schema: "fulfillment");
            migrationBuilder.DropTable(name: "items", schema: "fulfillment");
            migrationBuilder.DropTable(name: "fulfillments", schema: "fulfillment");
            migrationBuilder.DropTable(name: "payment_inbox", schema: "fulfillment");
            migrationBuilder.DropTable(name: "outbox_messages", schema: "fulfillment");
        }
    }
}
