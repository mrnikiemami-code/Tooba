using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Inventory.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-265-R2: additive schema for the exact identity of one requested release of an unconsumed order hold.
    /// <para>
    /// The existing <c>return_restock_inbox</c> row stores only the quantity that a RESTOCK actually performed, so it
    /// cannot identify a release REQUEST whose effect was clamped by the still-Held amount. This table records the
    /// immutable request (purpose, key, reservation, requested quantity) next to the actual effect and its outcome.
    /// No existing table, row or index is altered and no historical row is reinterpreted.
    /// </para>
    /// </summary>
    [DbContext(typeof(InventoryDbContext))]
    [Migration("20261114150000_AddHeldObligationReleases")]
    public partial class AddHeldObligationReleases : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "held_obligation_releases",
                schema: "inventory",
                columns: table => new
                {
                    idempotency_key = table.Column<string>(type: "character varying(190)", maxLength: 190, nullable: false),
                    purpose = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    released_quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_held_obligation_releases", x => x.idempotency_key);
                });

            migrationBuilder.CreateIndex(
                name: "ix_held_obligation_releases_reservation_id",
                schema: "inventory",
                table: "held_obligation_releases",
                column: "reservation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "held_obligation_releases",
                schema: "inventory");
        }
    }
}
