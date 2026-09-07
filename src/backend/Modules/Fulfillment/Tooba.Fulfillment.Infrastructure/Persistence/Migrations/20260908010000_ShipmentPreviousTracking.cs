using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Fulfillment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FulfillmentDbContext))]
    [Migration("20260908010000_ShipmentPreviousTracking")]
    public partial class ShipmentPreviousTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "previous_tracking_reference",
                schema: "fulfillment",
                table: "shipments",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "previous_tracking_reference",
                schema: "fulfillment",
                table: "shipments");
        }
    }
}
