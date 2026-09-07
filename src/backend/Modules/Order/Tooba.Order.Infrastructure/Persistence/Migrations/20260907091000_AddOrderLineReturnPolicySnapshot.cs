using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrderDbContext))]
    [Migration("20260907091000_AddOrderLineReturnPolicySnapshot")]
    public partial class AddOrderLineReturnPolicySnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_returnable_snapshot",
                schema: "order",
                table: "order_lines",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "return_window_days_snapshot",
                schema: "order",
                table: "order_lines",
                type: "integer",
                nullable: false,
                defaultValue: 7);

            migrationBuilder.AddColumn<string>(
                name: "return_policy_source_snapshot",
                schema: "order",
                table: "order_lines",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "return_policy_label_snapshot",
                schema: "order",
                table: "order_lines",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "order".order_lines
                SET return_policy_source_snapshot = COALESCE(return_policy_source_snapshot, 'platform_default'),
                    return_policy_label_snapshot = COALESCE(return_policy_label_snapshot, '۷ روز پس از تحویل')
                WHERE return_policy_source_snapshot IS NULL OR return_policy_label_snapshot IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "return_policy_label_snapshot",
                schema: "order",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "return_policy_source_snapshot",
                schema: "order",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "return_window_days_snapshot",
                schema: "order",
                table: "order_lines");

            migrationBuilder.DropColumn(
                name: "is_returnable_snapshot",
                schema: "order",
                table: "order_lines");
        }
    }
}
