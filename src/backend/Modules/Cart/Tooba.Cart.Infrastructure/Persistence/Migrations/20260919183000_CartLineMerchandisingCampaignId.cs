using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Cart.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Cart.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CartDbContext))]
    [Migration("20260919183000_CartLineMerchandisingCampaignId")]
    public partial class CartLineMerchandisingCampaignId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE cart.cart_lines
                ADD COLUMN IF NOT EXISTS merchandising_campaign_id uuid NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE cart.cart_lines
                DROP COLUMN IF EXISTS merchandising_campaign_id;
                """);
        }
    }
}
