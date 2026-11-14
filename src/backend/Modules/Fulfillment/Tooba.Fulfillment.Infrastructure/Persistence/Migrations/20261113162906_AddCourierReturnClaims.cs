using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCourierReturnClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "courier_return_claims",
                schema: "fulfillment",
                columns: table => new
                {
                    courier_return_claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fulfillment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipment_return_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurrence_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    return_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    active_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    settled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    released_held_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    lines_payload = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_courier_return_claims", x => x.courier_return_claim_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_courier_return_claims_fulfillment_id",
                schema: "fulfillment",
                table: "courier_return_claims",
                column: "fulfillment_id");

            migrationBuilder.CreateIndex(
                name: "ix_courier_return_claims_return_request_id",
                schema: "fulfillment",
                table: "courier_return_claims",
                column: "return_request_id",
                filter: "return_request_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_courier_return_claims_shipment_id",
                schema: "fulfillment",
                table: "courier_return_claims",
                column: "shipment_id");

            migrationBuilder.CreateIndex(
                name: "ix_courier_return_claims_shipment_return_receipt_id",
                schema: "fulfillment",
                table: "courier_return_claims",
                column: "shipment_return_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ux_courier_return_claims_active_key",
                schema: "fulfillment",
                table: "courier_return_claims",
                column: "active_key",
                unique: true,
                filter: "active_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_courier_return_claims_fulfillment_occurrence",
                schema: "fulfillment",
                table: "courier_return_claims",
                columns: new[] { "fulfillment_id", "occurrence_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "courier_return_claims",
                schema: "fulfillment");
        }
    }
}
