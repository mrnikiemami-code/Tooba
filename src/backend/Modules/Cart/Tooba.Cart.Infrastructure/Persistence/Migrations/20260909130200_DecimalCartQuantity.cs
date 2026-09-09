using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Cart.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Cart.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CartDbContext))]
    [Migration("20260909130200_DecimalCartQuantity")]
    public partial class DecimalCartQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE cart.cart_lines
                ALTER COLUMN quantity TYPE numeric(18,6)
                USING quantity::numeric(18,6);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE cart.cart_lines
                ALTER COLUMN quantity TYPE integer
                USING round(quantity)::integer;
                """);
        }
    }
}
