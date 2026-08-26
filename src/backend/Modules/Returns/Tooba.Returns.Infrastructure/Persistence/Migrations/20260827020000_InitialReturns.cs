using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Returns.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "returns");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "returns",
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
                name: "return_requests",
                schema: "returns",
                columns: table => new
                {
                    return_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checkout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    refund_amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_return_requests", x => x.return_request_id));

            migrationBuilder.CreateIndex(
                name: "ix_return_requests_checkout_id",
                schema: "returns",
                table: "return_requests",
                column: "checkout_id");

            migrationBuilder.CreateIndex(
                name: "ix_return_requests_idempotency_key",
                schema: "returns",
                table: "return_requests",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_return_requests_requested_by_user_id",
                schema: "returns",
                table: "return_requests",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_return_requests_seller_order_id",
                schema: "returns",
                table: "return_requests",
                column: "seller_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_return_requests_seller_party_id",
                schema: "returns",
                table: "return_requests",
                column: "seller_party_id");

            migrationBuilder.CreateTable(
                name: "return_items",
                schema: "returns",
                columns: table => new
                {
                    return_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price_snapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table => table.PrimaryKey("pk_return_items", x => x.return_item_id));

            migrationBuilder.CreateIndex(
                name: "ix_return_items_order_line_id",
                schema: "returns",
                table: "return_items",
                column: "order_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_return_items_return_request_id",
                schema: "returns",
                table: "return_items",
                column: "return_request_id");

            migrationBuilder.CreateTable(
                name: "refund_attempts",
                schema: "returns",
                columns: table => new
                {
                    refund_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    failure_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("pk_refund_attempts", x => x.refund_attempt_id));

            migrationBuilder.CreateIndex(
                name: "ix_refund_attempts_idempotency_key",
                schema: "returns",
                table: "refund_attempts",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refund_attempts_return_request_id",
                schema: "returns",
                table: "refund_attempts",
                column: "return_request_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "refund_attempts", schema: "returns");
            migrationBuilder.DropTable(name: "return_items", schema: "returns");
            migrationBuilder.DropTable(name: "return_requests", schema: "returns");
            migrationBuilder.DropTable(name: "outbox_messages", schema: "returns");
        }
    }
}
