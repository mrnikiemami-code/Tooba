using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-265-R1: additive schema for source-exact courier-origin claim closure.
    /// <para>
    /// <c>courier_return_claim_lines</c> carries ONE row per claimed (order line, condition) with its own active
    /// capacity key, so the database — not an in-memory aggregate list — refuses a second active claim of the same
    /// physically received quantity. <c>courier_return_claim_allocations</c> records, per approved claim, exactly
    /// which source receipt/line/quantity maps to which damaged decision, which not-yet-dispatched replacement parcel
    /// was cancelled and which held reservation was released, so an approval can never release a sibling line's hold.
    /// No existing table or row is altered, and no historical claim is reinterpreted.
    /// </para>
    /// </summary>
    [DbContext(typeof(FulfillmentDbContext))]
    [Migration("20261114130000_AddCourierReturnClaimLinesAndAllocations")]
    public partial class AddCourierReturnClaimLinesAndAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "courier_return_claim_lines",
                schema: "fulfillment",
                columns: table => new
                {
                    claim_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    condition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    active_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_courier_return_claim_lines", x => x.claim_line_id);
                });

            migrationBuilder.CreateTable(
                name: "courier_return_claim_allocations",
                schema: "fulfillment",
                columns: table => new
                {
                    allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    condition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    damaged_resolution_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_replacement_shipment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    held_reservation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    released_quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    state = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_courier_return_claim_allocations", x => x.allocation_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_courier_return_claim_lines_claim_id",
                schema: "fulfillment",
                table: "courier_return_claim_lines",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "ux_courier_return_claim_lines_active_key",
                schema: "fulfillment",
                table: "courier_return_claim_lines",
                column: "active_key",
                unique: true,
                filter: "active_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_courier_return_claim_allocations_claim_id",
                schema: "fulfillment",
                table: "courier_return_claim_allocations",
                column: "claim_id");

            migrationBuilder.CreateIndex(
                name: "ix_courier_return_claim_alloc_cancelled_parcel",
                schema: "fulfillment",
                table: "courier_return_claim_allocations",
                column: "cancelled_replacement_shipment_id",
                filter: "cancelled_replacement_shipment_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_courier_return_claim_allocations_held_reservation_id",
                schema: "fulfillment",
                table: "courier_return_claim_allocations",
                column: "held_reservation_id",
                filter: "held_reservation_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "courier_return_claim_allocations",
                schema: "fulfillment");

            migrationBuilder.DropTable(
                name: "courier_return_claim_lines",
                schema: "fulfillment");
        }
    }
}
