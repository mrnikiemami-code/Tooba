using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrderDbContext))]
    [Migration("20260908010000_SellerOrderCancelSnapshot")]
    public partial class SellerOrderCancelSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancelled_from_status",
                schema: "order",
                table: "seller_orders",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_restored_at",
                schema: "order",
                table: "seller_orders",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_restored_at",
                schema: "order",
                table: "seller_orders");

            migrationBuilder.DropColumn(
                name: "cancelled_from_status",
                schema: "order",
                table: "seller_orders");
        }
    }
}
