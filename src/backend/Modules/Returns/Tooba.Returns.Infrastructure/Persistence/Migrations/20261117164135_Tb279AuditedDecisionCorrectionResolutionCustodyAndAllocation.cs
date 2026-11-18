using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Returns.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tb279AuditedDecisionCorrectionResolutionCustodyAndAllocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedAmount",
                schema: "returns",
                table: "return_requests",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "return_delivery_allocations",
                schema: "returns",
                columns: table => new
                {
                    return_allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    slice_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    released_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_return_delivery_allocations", x => x.return_allocation_id);
                    table.CheckConstraint("ck_return_delivery_allocations_quantity_positive", "quantity > 0");
                    table.CheckConstraint("ck_return_delivery_allocations_slice_present", "length(btrim(slice_key)) > 0");
                    table.ForeignKey(
                        name: "fk_return_delivery_allocations_items_owning_case",
                        columns: x => new { x.return_request_id, x.return_item_id },
                        principalSchema: "returns",
                        principalTable: "return_items",
                        principalColumns: new[] { "return_request_id", "return_item_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_return_delivery_allocations_requests",
                        column: x => x.return_request_id,
                        principalSchema: "returns",
                        principalTable: "return_requests",
                        principalColumn: "return_request_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "return_item_custody",
                schema: "returns",
                columns: table => new
                {
                    return_custody_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    condition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    recipient_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proof_reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurrence_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_return_item_custody", x => x.return_custody_id);
                    table.CheckConstraint("ck_return_item_custody_condition_defined", "condition IN ('Usable', 'Damaged')");
                    table.CheckConstraint("ck_return_item_custody_proof_present", "length(btrim(proof_reference)) > 0");
                    table.CheckConstraint("ck_return_item_custody_quantity_positive", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_return_item_custody_items_owning_case",
                        columns: x => new { x.return_request_id, x.return_item_id },
                        principalSchema: "returns",
                        principalTable: "return_items",
                        principalColumns: new[] { "return_request_id", "return_item_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_return_item_custody_requests",
                        column: x => x.return_request_id,
                        principalSchema: "returns",
                        principalTable: "return_requests",
                        principalColumn: "return_request_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "return_item_resolutions",
                schema: "returns",
                columns: table => new
                {
                    return_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    resolution_version = table.Column<int>(type: "integer", nullable: false),
                    accepted_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    rejected_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurrence_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_return_item_resolutions", x => new { x.return_request_id, x.return_item_id, x.resolution_version });
                    table.CheckConstraint("ck_return_item_resolutions_accepted_non_negative", "accepted_quantity >= 0");
                    table.CheckConstraint("ck_return_item_resolutions_reason_present", "length(btrim(reason)) > 0");
                    table.CheckConstraint("ck_return_item_resolutions_rejected_non_negative", "rejected_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_return_item_resolutions_items_owning_case",
                        columns: x => new { x.return_request_id, x.return_item_id },
                        principalSchema: "returns",
                        principalTable: "return_items",
                        principalColumns: new[] { "return_request_id", "return_item_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_return_item_resolutions_requests",
                        column: x => x.return_request_id,
                        principalSchema: "returns",
                        principalTable: "return_requests",
                        principalColumn: "return_request_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "return_request_decision_revisions",
                schema: "returns",
                columns: table => new
                {
                    return_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    revision_version = table.Column<int>(type: "integer", nullable: false),
                    decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurrence_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_return_request_decision_revisions", x => new { x.return_request_id, x.revision_version });
                    table.ForeignKey(
                        name: "fk_return_request_decision_revisions_requests",
                        column: x => x.return_request_id,
                        principalSchema: "returns",
                        principalTable: "return_requests",
                        principalColumn: "return_request_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_return_delivery_allocations_owning_case_slice",
                schema: "returns",
                table: "return_delivery_allocations",
                columns: new[] { "return_request_id", "return_item_id", "slice_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_return_delivery_allocations_owning_item",
                schema: "returns",
                table: "return_delivery_allocations",
                columns: new[] { "return_request_id", "return_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_return_delivery_allocations_owning_slice",
                schema: "returns",
                table: "return_delivery_allocations",
                columns: new[] { "order_line_id", "slice_key" });

            migrationBuilder.CreateIndex(
                name: "ix_return_item_custody_occurrence_key",
                schema: "returns",
                table: "return_item_custody",
                columns: new[] { "return_request_id", "occurrence_key" });

            migrationBuilder.CreateIndex(
                name: "ix_return_item_custody_owning_item",
                schema: "returns",
                table: "return_item_custody",
                columns: new[] { "return_request_id", "return_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_return_item_resolutions_occurrence_key",
                schema: "returns",
                table: "return_item_resolutions",
                columns: new[] { "return_request_id", "occurrence_key" });

            migrationBuilder.CreateIndex(
                name: "ix_return_request_decision_revisions_occurrence_key",
                schema: "returns",
                table: "return_request_decision_revisions",
                columns: new[] { "return_request_id", "occurrence_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "return_delivery_allocations",
                schema: "returns");

            migrationBuilder.DropTable(
                name: "return_item_custody",
                schema: "returns");

            migrationBuilder.DropTable(
                name: "return_item_resolutions",
                schema: "returns");

            migrationBuilder.DropTable(
                name: "return_request_decision_revisions",
                schema: "returns");

            migrationBuilder.DropColumn(
                name: "ApprovedAmount",
                schema: "returns",
                table: "return_requests");
        }
    }
}
