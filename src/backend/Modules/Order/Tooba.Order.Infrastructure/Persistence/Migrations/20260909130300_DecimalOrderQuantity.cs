using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrderDbContext))]
    [Migration("20260909130300_DecimalOrderQuantity")]
    public partial class DecimalOrderQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "order".order_lines
                ALTER COLUMN quantity TYPE numeric(18,6)
                USING quantity::numeric(18,6);
                """);
            migrationBuilder.AddColumn<int>(
                name: "quantity_decimal_places_snapshot",
                schema: "order",
                table: "order_lines",
                type: "integer",
                nullable: false,
                defaultValue: 0);
            migrationBuilder.AddColumn<decimal>(
                name: "quantity_step_snapshot",
                schema: "order",
                table: "order_lines",
                type: "numeric(18,6)",
                nullable: true);
            migrationBuilder.AddColumn<Guid>(
                name: "unit_of_measure_id_snapshot",
                schema: "order",
                table: "order_lines",
                type: "uuid",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "unit_code_snapshot",
                schema: "order",
                table: "order_lines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "unit_display_snapshot",
                schema: "order",
                table: "order_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "quantity_decimal_places_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.DropColumn(name: "quantity_step_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.DropColumn(name: "unit_of_measure_id_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.DropColumn(name: "unit_code_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.DropColumn(name: "unit_display_snapshot", schema: "order", table: "order_lines");
            migrationBuilder.Sql("""
                ALTER TABLE "order".order_lines
                ALTER COLUMN quantity TYPE integer
                USING round(quantity)::integer;
                """);
        }
    }
}
