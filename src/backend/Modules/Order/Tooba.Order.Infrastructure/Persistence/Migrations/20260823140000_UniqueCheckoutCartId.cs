using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// یکتایی پایدار <c>cart_id</c> روی checkout تا دو Submit هم‌زمان نتوانند دو گروه سفارش برای یک سبد بسازند.
    /// بدون ویژگی <see cref="MigrationAttribute"/> این کلاس به تاریخچهٔ EF وصل نمی‌شد و قید در Postgres اعمال نمی‌شد.
    /// </summary>
    [DbContext(typeof(OrderDbContext))]
    [Migration("20260823140000_UniqueCheckoutCartId")]
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
