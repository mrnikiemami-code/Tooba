using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Returns.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-274: additive receipts/inspection schema for customer-delivered returns, plus the reviewed rejection reason
    /// on the existing winning-decision row.
    /// <para>
    /// Nothing existing is renamed or dropped: <c>reason</c> is a new nullable column on
    /// <c>returns.return_request_decisions</c>, and the two new tables own the physical receiving facts
    /// (<c>customer_return_receipts</c> = one occurrence per case, <c>customer_return_receipt_lines</c> = one
    /// (item, condition) quantity per occurrence). Identifiers are the persisted snake_case names, and the foreign keys
    /// reference the persisted principal key columns (<c>return_request_id</c>, <c>return_item_id</c>).
    /// </para>
    /// </summary>
    public partial class AddCustomerReceiptsAndDecisionReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "reason",
                schema: "returns",
                table: "return_request_decisions",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "customer_return_receipts",
                schema: "returns",
                columns: table => new
                {
                    return_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurrence_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    received_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_return_receipts", x => x.return_receipt_id);
                    table.ForeignKey(
                        name: "fk_customer_return_receipts_return_requests_return_request_id",
                        column: x => x.return_request_id,
                        principalSchema: "returns",
                        principalTable: "return_requests",
                        principalColumn: "return_request_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "customer_return_receipt_lines",
                schema: "returns",
                columns: table => new
                {
                    return_receipt_line_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    condition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_return_receipt_lines", x => x.return_receipt_line_id);
                    table.ForeignKey(
                        name: "fk_customer_return_receipt_lines_receipts_return_receipt_id",
                        column: x => x.return_receipt_id,
                        principalSchema: "returns",
                        principalTable: "customer_return_receipts",
                        principalColumn: "return_receipt_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_customer_return_receipt_lines_return_items_return_item_id",
                        column: x => x.return_item_id,
                        principalSchema: "returns",
                        principalTable: "return_items",
                        principalColumn: "return_item_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_customer_return_receipt_lines_receipt_item_condition",
                schema: "returns",
                table: "customer_return_receipt_lines",
                columns: new[] { "return_receipt_id", "return_item_id", "condition" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_customer_return_receipt_lines_return_item_id",
                schema: "returns",
                table: "customer_return_receipt_lines",
                column: "return_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_return_receipt_lines_return_request_id",
                schema: "returns",
                table: "customer_return_receipt_lines",
                column: "return_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_return_receipts_return_request_id",
                schema: "returns",
                table: "customer_return_receipts",
                column: "return_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_return_receipts_return_request_id_occurrence_key",
                schema: "returns",
                table: "customer_return_receipts",
                columns: new[] { "return_request_id", "occurrence_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_return_receipt_lines",
                schema: "returns");

            migrationBuilder.DropTable(
                name: "customer_return_receipts",
                schema: "returns");

            migrationBuilder.DropColumn(
                name: "reason",
                schema: "returns",
                table: "return_request_decisions");
        }
    }
}
