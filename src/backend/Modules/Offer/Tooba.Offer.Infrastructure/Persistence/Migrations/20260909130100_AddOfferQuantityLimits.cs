using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Offer.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Offer.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OfferDbContext))]
    [Migration("20260909130100_AddOfferQuantityLimits")]
    public partial class AddOfferQuantityLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "minimum_order_quantity",
                schema: "offer",
                table: "offers",
                type: "numeric(18,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "maximum_order_quantity",
                schema: "offer",
                table: "offers",
                type: "numeric(18,6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "minimum_order_quantity", schema: "offer", table: "offers");
            migrationBuilder.DropColumn(name: "maximum_order_quantity", schema: "offer", table: "offers");
        }
    }
}
