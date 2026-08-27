using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrderDbContext))]
    [Migration("20260827180000_AddOrderLineCategoryIdSnapshot")]
    public partial class AddOrderLineCategoryIdSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "category_id_snapshot",
                schema: "order",
                table: "order_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_lines_category_id_snapshot",
                schema: "order",
                table: "order_lines",
                column: "category_id_snapshot");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_order_lines_category_id_snapshot",
                schema: "order",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "category_id_snapshot",
                schema: "order",
                table: "order_lines");
        }
    }
}
