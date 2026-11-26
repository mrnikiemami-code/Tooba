using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Inventory.Infrastructure.Persistence.Migrations
{
    /// <summary>R10: offer-wide allocation and durable Inventory owner facts.</summary>
    [DbContext(typeof(InventoryDbContext))]
    [Migration("20261126180000_R10AllocationAndOwnerFacts")]
    public partial class R10AllocationAndOwnerFacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "offer_allocations",
                schema: "inventory",
                columns: table => new
                {
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    held = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    revision = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_offer_allocations", x => x.offer_id));

            migrationBuilder.CreateTable(
                name: "offer_inventory_changes",
                schema: "inventory",
                columns: table => new
                {
                    fact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    old_on_hand = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    new_on_hand = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    old_allocation = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    new_allocation = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: true),
                    reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                },
                constraints: table => table.PrimaryKey("pk_offer_inventory_changes", x => x.fact_id));

            migrationBuilder.CreateIndex(
                name: "ix_offer_inventory_changes_offer_id_occurred_at",
                schema: "inventory",
                table: "offer_inventory_changes",
                columns: new[] { "offer_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_offer_inventory_changes_operation_id",
                schema: "inventory",
                table: "offer_inventory_changes",
                column: "operation_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "offer_allocations", schema: "inventory");
            migrationBuilder.DropTable(name: "offer_inventory_changes", schema: "inventory");
        }
    }
}
