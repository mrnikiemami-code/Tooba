using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Inventory.Infrastructure.Persistence.Migrations
{
    /// <summary>Distinguishes new sale holds from existing-order recovery without relabeling historical reservations.</summary>
    [DbContext(typeof(InventoryDbContext))]
    [Migration("20261126190000_R10ReservationPurpose")]
    public partial class R10ReservationPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "purpose", schema: "inventory", table: "reservations",
                type: "character varying(32)", maxLength: 32, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "purpose", schema: "inventory", table: "reservations");
        }
    }
}
