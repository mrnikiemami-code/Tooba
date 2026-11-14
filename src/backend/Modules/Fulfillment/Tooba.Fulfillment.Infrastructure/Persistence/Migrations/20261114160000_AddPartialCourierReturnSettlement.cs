using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-265-R2: additive schema for a PARTIAL courier-origin case selection.
    /// <para>
    /// A damaged-goods decision may be larger than the portion a Returns case selects (for example 3 damaged units with
    /// a retained replacement hold of 3, of which the customer returns 1). The decision therefore remembers how much of
    /// its decided quantity was already settled into approved cases, so the unselected remainder stays a live, held
    /// replacement obligation instead of being cancelled or refunded by the case. Existing rows read as
    /// <c>settled = 0</c>, which is exactly their historical meaning: nothing was ever partially settled before.
    /// </para>
    /// <para>
    /// The per-line capacity index also stops being UNIQUE: two lawful claims of disjoint quantities on the same
    /// (receipt, line, condition) must both be accepted, and the real serialization authority is the owning
    /// Serializable transaction plus the Fulfillment row lock plus the aggregate's free-quantity computation.
    /// </para>
    /// </summary>
    [DbContext(typeof(FulfillmentDbContext))]
    [Migration("20261114160000_AddPartialCourierReturnSettlement")]
    public partial class AddPartialCourierReturnSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "case_settled_quantity",
                schema: "fulfillment",
                table: "damaged_goods_resolutions",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.DropIndex(
                name: "ux_courier_return_claim_lines_active_key",
                schema: "fulfillment",
                table: "courier_return_claim_lines");

            migrationBuilder.CreateIndex(
                name: "ix_courier_return_claim_lines_active_key",
                schema: "fulfillment",
                table: "courier_return_claim_lines",
                column: "active_key",
                filter: "active_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_courier_return_claim_lines_active_key",
                schema: "fulfillment",
                table: "courier_return_claim_lines");

            migrationBuilder.CreateIndex(
                name: "ux_courier_return_claim_lines_active_key",
                schema: "fulfillment",
                table: "courier_return_claim_lines",
                column: "active_key",
                unique: true,
                filter: "active_key IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "case_settled_quantity",
                schema: "fulfillment",
                table: "damaged_goods_resolutions");
        }
    }
}
