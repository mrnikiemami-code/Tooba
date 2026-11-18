using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Returns.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-279-R1 (F2/F3): one bounded ADDITIVE migration. It adds the explicit per-condition resolution portions, the two
    /// per-portion physical completion facts and the custody proof kind — every column nullable, so no stored row is
    /// rewritten, backfilled or dropped and a pre-R1 row is honestly read as "portions unknown".
    /// </summary>
    public partial class Tb279R1PerConditionPortionsAndCustodyProof : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "damaged_recorded_quantity",
                schema: "returns",
                table: "return_items",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "usable_restocked_quantity",
                schema: "returns",
                table: "return_items",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "accepted_damaged_quantity",
                schema: "returns",
                table: "return_item_resolutions",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "accepted_usable_quantity",
                schema: "returns",
                table: "return_item_resolutions",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rejected_received_damaged_quantity",
                schema: "returns",
                table: "return_item_resolutions",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rejected_received_usable_quantity",
                schema: "returns",
                table: "return_item_resolutions",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "rejected_unreceived_quantity",
                schema: "returns",
                table: "return_item_resolutions",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "proof_kind",
                schema: "returns",
                table: "return_item_custody",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "damaged_recorded_quantity",
                schema: "returns",
                table: "return_items");

            migrationBuilder.DropColumn(
                name: "usable_restocked_quantity",
                schema: "returns",
                table: "return_items");

            migrationBuilder.DropColumn(
                name: "accepted_damaged_quantity",
                schema: "returns",
                table: "return_item_resolutions");

            migrationBuilder.DropColumn(
                name: "accepted_usable_quantity",
                schema: "returns",
                table: "return_item_resolutions");

            migrationBuilder.DropColumn(
                name: "rejected_received_damaged_quantity",
                schema: "returns",
                table: "return_item_resolutions");

            migrationBuilder.DropColumn(
                name: "rejected_received_usable_quantity",
                schema: "returns",
                table: "return_item_resolutions");

            migrationBuilder.DropColumn(
                name: "rejected_unreceived_quantity",
                schema: "returns",
                table: "return_item_resolutions");

            migrationBuilder.DropColumn(
                name: "proof_kind",
                schema: "returns",
                table: "return_item_custody");
        }
    }
}
