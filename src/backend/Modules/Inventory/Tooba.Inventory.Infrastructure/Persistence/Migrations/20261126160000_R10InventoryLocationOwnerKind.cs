using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Inventory.Infrastructure.Persistence.Migrations
{
    /// <summary>R10: explicit owner kind; existing locations are store-owned, never null-shared.</summary>
    [DbContext(typeof(InventoryDbContext))]
    [Migration("20261126160000_R10InventoryLocationOwnerKind")]
    public partial class R10InventoryLocationOwnerKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "owner_kind",
                schema: "inventory",
                table: "locations",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Store");

            migrationBuilder.CreateIndex(
                name: "ix_locations_owner_kind_seller_party_id",
                schema: "inventory",
                table: "locations",
                columns: new[] { "owner_kind", "seller_party_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_locations_owner_kind_seller_party_id",
                schema: "inventory",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "owner_kind",
                schema: "inventory",
                table: "locations");
        }
    }
}
