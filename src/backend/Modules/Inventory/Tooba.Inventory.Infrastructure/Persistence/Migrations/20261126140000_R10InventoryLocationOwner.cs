using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Inventory.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// R10: optional seller ownership for a storage location. Null keeps a store-owned shared location
    /// available to every seller; a non-null value restricts provisioning to that seller.
    /// </summary>
    [DbContext(typeof(InventoryDbContext))]
    [Migration("20261126140000_R10InventoryLocationOwner")]
    public partial class R10InventoryLocationOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "seller_party_id",
                schema: "inventory",
                table: "locations",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "seller_party_id",
                schema: "inventory",
                table: "locations");
        }
    }
}
