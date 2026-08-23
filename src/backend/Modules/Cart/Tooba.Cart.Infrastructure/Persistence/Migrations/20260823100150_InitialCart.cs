using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Cart.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "cart");

            migrationBuilder.CreateTable(
                name: "carts",
                schema: "cart",
                columns: table => new
                {
                    cart_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    access_kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    guest_credential_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    market = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    conversion_intent = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_carts", x => x.cart_id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "cart",
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
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cart_lines",
                schema: "cart",
                columns: table => new
                {
                    line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cart_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    catalog_variant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quoted_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    quoted_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    quoted_tax_exclusive = table.Column<bool>(type: "boolean", nullable: false),
                    price_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quoted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cart_lines", x => x.line_id);
                    table.ForeignKey(
                        name: "fk_cart_lines_carts_cart_id",
                        column: x => x.cart_id,
                        principalSchema: "cart",
                        principalTable: "carts",
                        principalColumn: "cart_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cart_lines_cart_id_offer_id",
                schema: "cart",
                table: "cart_lines",
                columns: new[] { "cart_id", "offer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_carts_expires_at",
                schema: "cart",
                table: "carts",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_carts_owner_user_id",
                schema: "cart",
                table: "carts",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                schema: "cart",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL AND dead_lettered_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cart_lines",
                schema: "cart");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "cart");

            migrationBuilder.DropTable(
                name: "carts",
                schema: "cart");
        }
    }
}
