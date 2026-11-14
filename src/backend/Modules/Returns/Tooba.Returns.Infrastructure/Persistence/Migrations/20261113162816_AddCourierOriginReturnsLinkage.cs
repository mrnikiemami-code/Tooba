using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Returns.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-265: courier-origin Returns linkage. Additive only — every existing return request stays
    /// <c>CustomerDelivered</c>/<c>None</c> and every existing item stays <c>Usable</c>/<c>None</c>, so historical
    /// cases keep their exact meaning and nothing is backfilled or reinterpreted.
    /// </summary>
    public partial class AddCourierOriginReturnsLinkage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "origin",
                schema: "returns",
                table: "return_requests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "CustomerDelivered");

            migrationBuilder.AddColumn<Guid>(
                name: "source_fulfillment_id",
                schema: "returns",
                table: "return_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_shipment_id",
                schema: "returns",
                table: "return_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_receipt_id",
                schema: "returns",
                table: "return_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "source_occurrence_key",
                schema: "returns",
                table: "return_requests",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_claim_id",
                schema: "returns",
                table: "return_requests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "obligation_closed_at",
                schema: "returns",
                table: "return_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "stock_settled_at",
                schema: "returns",
                table: "return_requests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "closure_phase",
                schema: "returns",
                table: "return_requests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<decimal>(
                name: "released_held_quantity",
                schema: "returns",
                table: "return_requests",
                type: "numeric(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "condition",
                schema: "returns",
                table: "return_items",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Usable");

            migrationBuilder.AddColumn<string>(
                name: "disposition",
                schema: "returns",
                table: "return_items",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<Guid>(
                name: "source_shipment_id",
                schema: "returns",
                table: "return_items",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "source_receipt_id",
                schema: "returns",
                table: "return_items",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "source_receipt_id", schema: "returns", table: "return_items");
            migrationBuilder.DropColumn(name: "source_shipment_id", schema: "returns", table: "return_items");
            migrationBuilder.DropColumn(name: "disposition", schema: "returns", table: "return_items");
            migrationBuilder.DropColumn(name: "condition", schema: "returns", table: "return_items");

            migrationBuilder.DropColumn(name: "released_held_quantity", schema: "returns", table: "return_requests");
            migrationBuilder.DropColumn(name: "closure_phase", schema: "returns", table: "return_requests");
            migrationBuilder.DropColumn(name: "stock_settled_at", schema: "returns", table: "return_requests");
            migrationBuilder.DropColumn(name: "obligation_closed_at", schema: "returns", table: "return_requests");
            migrationBuilder.DropColumn(name: "source_claim_id", schema: "returns", table: "return_requests");
            migrationBuilder.DropColumn(name: "source_occurrence_key", schema: "returns", table: "return_requests");
            migrationBuilder.DropColumn(name: "source_receipt_id", schema: "returns", table: "return_requests");
            migrationBuilder.DropColumn(name: "source_shipment_id", schema: "returns", table: "return_requests");
            migrationBuilder.DropColumn(name: "source_fulfillment_id", schema: "returns", table: "return_requests");
            migrationBuilder.DropColumn(name: "origin", schema: "returns", table: "return_requests");
        }
    }
}
