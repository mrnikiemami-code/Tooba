using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrderDbContext))]
    [Migration("20260909140000_AddInvoiceHeaderAggregates")]
    public partial class AddInvoiceHeaderAggregates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "order".order_lines
                    ADD COLUMN IF NOT EXISTS duty_amount_snapshot numeric(19,4) NOT NULL DEFAULT 0;
                ALTER TABLE "order".seller_orders
                    ADD COLUMN IF NOT EXISTS total_item_count integer NOT NULL DEFAULT 0;
                ALTER TABLE "order".seller_orders
                    ADD COLUMN IF NOT EXISTS total_quantity numeric(18,6) NOT NULL DEFAULT 0;
                ALTER TABLE "order".seller_orders
                    ADD COLUMN IF NOT EXISTS net_amount_before_tax numeric(19,4) NOT NULL DEFAULT 0;
                ALTER TABLE "order".seller_orders
                    ADD COLUMN IF NOT EXISTS total_duty_amount numeric(19,4) NOT NULL DEFAULT 0;
                ALTER TABLE "order".seller_orders
                    ADD COLUMN IF NOT EXISTS total_tax_and_duty_amount numeric(19,4) NOT NULL DEFAULT 0;
                ALTER TABLE "order".seller_orders
                    ADD COLUMN IF NOT EXISTS rounding_mode_used character varying(32) NOT NULL DEFAULT 'Nearest';
                ALTER TABLE "order".seller_orders
                    ADD COLUMN IF NOT EXISTS money_decimal_places_used integer NOT NULL DEFAULT 0;
                UPDATE "order".seller_orders so
                SET total_item_count = sub.items,
                    total_quantity = sub.qty,
                    net_amount_before_tax = so.subtotal_snapshot - so.discount_snapshot,
                    total_duty_amount = 0,
                    total_tax_and_duty_amount = so.tax_snapshot,
                    rounding_mode_used = 'Nearest',
                    money_decimal_places_used = CASE
                        WHEN upper(so.currency) IN ('IRR', 'JPY', 'KRW') THEN 0
                        ELSE 2
                    END
                FROM (
                    SELECT seller_order_id,
                           COUNT(*)::int AS items,
                           COALESCE(SUM(quantity), 0) AS qty
                    FROM "order".order_lines
                    GROUP BY seller_order_id
                ) sub
                WHERE so.seller_order_id = sub.seller_order_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "order".seller_orders DROP COLUMN IF EXISTS money_decimal_places_used;
                ALTER TABLE "order".seller_orders DROP COLUMN IF EXISTS rounding_mode_used;
                ALTER TABLE "order".seller_orders DROP COLUMN IF EXISTS total_tax_and_duty_amount;
                ALTER TABLE "order".seller_orders DROP COLUMN IF EXISTS total_duty_amount;
                ALTER TABLE "order".seller_orders DROP COLUMN IF EXISTS net_amount_before_tax;
                ALTER TABLE "order".seller_orders DROP COLUMN IF EXISTS total_quantity;
                ALTER TABLE "order".seller_orders DROP COLUMN IF EXISTS total_item_count;
                ALTER TABLE "order".order_lines DROP COLUMN IF EXISTS duty_amount_snapshot;
                """);
        }
    }
}
