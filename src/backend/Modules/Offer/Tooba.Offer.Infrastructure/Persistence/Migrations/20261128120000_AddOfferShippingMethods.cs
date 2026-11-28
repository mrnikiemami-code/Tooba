using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Offer.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Offer.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-296: additive offer-owned shipping method binding. Adds one nullable selection-mode column and one
    /// link table. Nothing is backfilled and no legacy offer receives invented method rows.
    /// </summary>
    [DbContext(typeof(OfferDbContext))]
    [Migration("20261128120000_AddOfferShippingMethods")]
    public partial class AddOfferShippingMethods : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "shipping_selection_mode",
                schema: "offer",
                table: "offers",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "offer_shipping_methods",
                schema: "offer",
                columns: table => new
                {
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    method_id = table.Column<Guid>(type: "uuid", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offer_shipping_methods", x => new { x.offer_id, x.method_id });
                    table.ForeignKey(
                        name: "fk_offer_shipping_methods_offers_offer_id",
                        column: x => x.offer_id,
                        principalSchema: "offer",
                        principalTable: "offers",
                        principalColumn: "offer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_offer_shipping_methods_offer_id",
                schema: "offer",
                table: "offer_shipping_methods",
                column: "offer_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "offer_shipping_methods", schema: "offer");
            migrationBuilder.DropColumn(name: "shipping_selection_mode", schema: "offer", table: "offers");
        }
    }
}
