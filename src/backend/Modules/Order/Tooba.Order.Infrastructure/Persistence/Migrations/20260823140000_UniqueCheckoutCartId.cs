using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueCheckoutCartId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_checkouts_cart_id",
                schema: "order",
                table: "checkouts",
                column: "cart_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_checkouts_cart_id",
                schema: "order",
                table: "checkouts");
        }
    }
}
