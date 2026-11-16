using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Returns.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BoundedReceiptIntegrityConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_return_receipt_lines_receipts_return_receipt_id",
                schema: "returns",
                table: "customer_return_receipt_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_return_receipt_lines_return_items_return_item_id",
                schema: "returns",
                table: "customer_return_receipt_lines");

            migrationBuilder.AddUniqueConstraint(
                name: "ak_return_items_return_request_id_return_item_id",
                schema: "returns",
                table: "return_items",
                columns: new[] { "return_request_id", "return_item_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "ak_customer_return_receipts_return_request_id_return_receipt_id",
                schema: "returns",
                table: "customer_return_receipts",
                columns: new[] { "return_request_id", "return_receipt_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_return_receipt_lines_owning_item",
                schema: "returns",
                table: "customer_return_receipt_lines",
                columns: new[] { "return_request_id", "return_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_customer_return_receipt_lines_owning_receipt",
                schema: "returns",
                table: "customer_return_receipt_lines",
                columns: new[] { "return_request_id", "return_receipt_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_return_receipt_lines_condition_defined",
                schema: "returns",
                table: "customer_return_receipt_lines",
                sql: "condition IN ('Usable', 'Damaged')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_customer_return_receipt_lines_quantity_positive",
                schema: "returns",
                table: "customer_return_receipt_lines",
                sql: "quantity > 0");

            migrationBuilder.AddForeignKey(
                name: "fk_customer_return_receipt_lines_receipts_owning_case",
                schema: "returns",
                table: "customer_return_receipt_lines",
                columns: new[] { "return_request_id", "return_receipt_id" },
                principalSchema: "returns",
                principalTable: "customer_return_receipts",
                principalColumns: new[] { "return_request_id", "return_receipt_id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_return_receipt_lines_return_items_owning_case",
                schema: "returns",
                table: "customer_return_receipt_lines",
                columns: new[] { "return_request_id", "return_item_id" },
                principalSchema: "returns",
                principalTable: "return_items",
                principalColumns: new[] { "return_request_id", "return_item_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_customer_return_receipt_lines_receipts_owning_case",
                schema: "returns",
                table: "customer_return_receipt_lines");

            migrationBuilder.DropForeignKey(
                name: "fk_customer_return_receipt_lines_return_items_owning_case",
                schema: "returns",
                table: "customer_return_receipt_lines");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_return_items_return_request_id_return_item_id",
                schema: "returns",
                table: "return_items");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_customer_return_receipts_return_request_id_return_receipt_id",
                schema: "returns",
                table: "customer_return_receipts");

            migrationBuilder.DropIndex(
                name: "ix_customer_return_receipt_lines_owning_item",
                schema: "returns",
                table: "customer_return_receipt_lines");

            migrationBuilder.DropIndex(
                name: "ix_customer_return_receipt_lines_owning_receipt",
                schema: "returns",
                table: "customer_return_receipt_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_return_receipt_lines_condition_defined",
                schema: "returns",
                table: "customer_return_receipt_lines");

            migrationBuilder.DropCheckConstraint(
                name: "ck_customer_return_receipt_lines_quantity_positive",
                schema: "returns",
                table: "customer_return_receipt_lines");

            migrationBuilder.AddForeignKey(
                name: "fk_customer_return_receipt_lines_receipts_return_receipt_id",
                schema: "returns",
                table: "customer_return_receipt_lines",
                column: "return_receipt_id",
                principalSchema: "returns",
                principalTable: "customer_return_receipts",
                principalColumn: "return_receipt_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_customer_return_receipt_lines_return_items_return_item_id",
                schema: "returns",
                table: "customer_return_receipt_lines",
                column: "return_item_id",
                principalSchema: "returns",
                principalTable: "return_items",
                principalColumn: "return_item_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
