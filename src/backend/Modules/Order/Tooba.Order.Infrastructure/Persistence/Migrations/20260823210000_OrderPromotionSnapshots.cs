using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrderDbContext))]
    [Migration("20260823210000_OrderPromotionSnapshots")]
    public partial class OrderPromotionSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "discount_amount_snapshot",
                schema: "order",
                table: "order_lines",
                type: "numeric(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
            migrationBuilder.AddColumn<string>(
                name: "discount_kind_snapshot",
                schema: "order",
                table: "order_lines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
            migrationBuilder.AddColumn<decimal>(
                name: "post_discount_tax_exclusive_snapshot",
                schema: "order",
                table: "order_lines",
                type: "numeric(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
            migrationBuilder.AddColumn<decimal>(
                name: "pre_discount_tax_exclusive_snapshot",
                schema: "order",
                table: "order_lines",
                type: "numeric(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "promotion_applied_at_snapshot",
                schema: "order",
                table: "order_lines",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "promotion_code_snapshot",
                schema: "order",
                table: "order_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "promotion_id_snapshot",
                schema: "order",
                table: "order_lines",
                type: "uuid",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "promotion_name_snapshot",
                schema: "order",
                table: "order_lines",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "discount_amount_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.DropColumn(name: "discount_kind_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.DropColumn(name: "post_discount_tax_exclusive_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.DropColumn(name: "pre_discount_tax_exclusive_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.DropColumn(name: "promotion_applied_at_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.DropColumn(name: "promotion_code_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.DropColumn(name: "promotion_id_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.DropColumn(name: "promotion_name_snapshot", schema: "order", table: "order_lines");
        }
    }
}
