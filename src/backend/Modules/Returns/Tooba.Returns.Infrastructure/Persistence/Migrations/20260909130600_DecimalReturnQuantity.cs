using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Returns.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Returns.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ReturnsDbContext))]
    [Migration("20260909130600_DecimalReturnQuantity")]
    public partial class DecimalReturnQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE returns.return_items
                ALTER COLUMN quantity TYPE numeric(18,6)
                USING quantity::numeric(18,6);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE returns.return_items
                ALTER COLUMN quantity TYPE integer
                USING round(quantity)::integer;
                """);
        }
    }
}
