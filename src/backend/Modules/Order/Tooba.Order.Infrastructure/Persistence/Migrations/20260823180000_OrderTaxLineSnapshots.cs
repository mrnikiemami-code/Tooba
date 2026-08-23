using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrderDbContext))]
    [Migration("20260823180000_OrderTaxLineSnapshots")]
    public partial class OrderTaxLineSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "tax_amount_snapshot",
                schema: "order",
                table: "order_lines",
                type: "numeric(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "tax_inclusive_snapshot",
                schema: "order",
                table: "order_lines",
                type: "numeric(19,4)",
                precision: 19,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "tax_outcome_snapshot",
                schema: "order",
                table: "order_lines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "tax_rate_snapshot",
                schema: "order",
                table: "order_lines",
                type: "numeric(19,8)",
                precision: 19,
                scale: 8,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "tax_rule_id_snapshot",
                schema: "order",
                table: "order_lines",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "tax_amount_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.DropColumn(name: "tax_inclusive_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.DropColumn(name: "tax_outcome_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.DropColumn(name: "tax_rate_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.DropColumn(name: "tax_rule_id_snapshot", schema: "order", table: "order_lines");
        }
    }
}
