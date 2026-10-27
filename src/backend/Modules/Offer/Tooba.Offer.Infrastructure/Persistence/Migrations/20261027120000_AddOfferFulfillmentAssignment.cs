using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Offer.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Offer.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Explicit fulfillment assignment on an Offer (TB-228): two nullable additive columns only.
    /// Nothing is backfilled — every existing listing stays unassigned on purpose.
    /// </summary>
    [DbContext(typeof(OfferDbContext))]
    [Migration("20261027120000_AddOfferFulfillmentAssignment")]
    public partial class AddOfferFulfillmentAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "fulfillment_responsibility",
                schema: "offer",
                table: "offers",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "fulfillment_shipping_type_id",
                schema: "offer",
                table: "offers",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "fulfillment_responsibility", schema: "offer", table: "offers");
            migrationBuilder.DropColumn(name: "fulfillment_shipping_type_id", schema: "offer", table: "offers");
        }
    }
}
