using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-127 — گروه‌های ارسال پایدار سفارش پذیرفته‌شده (Order-owned M2).
/// یک گروه = یک فروشنده + یک روش متعارف + هزینهٔ snapshot؛ هر خط سفارش دقیقاً در یک گروه.
/// جدول‌ها فقط Additive هستند و سفارش‌های قدیمی تک‌روشی با ستون‌های اسکالر خوانده می‌شوند.
/// </summary>
[DbContext(typeof(OrderDbContext))]
[Migration("20261016100000_AddOrderShippingGroups")]
public partial class AddOrderShippingGroups : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "order".order_shipping_groups
            (
                shipping_group_id uuid NOT NULL,
                checkout_id uuid NOT NULL,
                seller_order_id uuid NOT NULL,
                seller_party_id uuid NOT NULL,
                method_code character varying(64) NOT NULL,
                service_code character varying(64) NULL,
                option_code character varying(64) NULL,
                method_label character varying(128) NOT NULL,
                origin_context character varying(64) NULL,
                shipping_charge_snapshot numeric(18,2) NOT NULL,
                currency character varying(3) NOT NULL,
                minimum_delivery_date date NOT NULL,
                requested_delivery_date date NULL,
                requested_delivery_time_window character varying(32) NULL,
                lead_days integer NOT NULL,
                state character varying(32) NOT NULL,
                created_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_order_shipping_groups PRIMARY KEY (shipping_group_id),
                CONSTRAINT fk_order_shipping_groups_checkout FOREIGN KEY (checkout_id)
                    REFERENCES "order".checkouts (checkout_id) ON DELETE CASCADE,
                CONSTRAINT ck_order_shipping_groups_amount CHECK (shipping_charge_snapshot >= 0),
                CONSTRAINT ck_order_shipping_groups_currency CHECK (char_length(currency) = 3),
                CONSTRAINT ck_order_shipping_groups_lead_days CHECK (lead_days >= 0)
            );

            CREATE INDEX IF NOT EXISTS ix_order_shipping_groups_checkout
                ON "order".order_shipping_groups (checkout_id);

            CREATE INDEX IF NOT EXISTS ix_order_shipping_groups_seller_order
                ON "order".order_shipping_groups (seller_order_id);

            CREATE UNIQUE INDEX IF NOT EXISTS ux_order_shipping_groups_checkout_seller_method
                ON "order".order_shipping_groups (checkout_id, seller_party_id, method_code);

            CREATE TABLE IF NOT EXISTS "order".order_shipping_group_lines
            (
                shipping_group_id uuid NOT NULL,
                order_line_id uuid NOT NULL,
                CONSTRAINT pk_order_shipping_group_lines PRIMARY KEY (shipping_group_id, order_line_id),
                CONSTRAINT fk_order_shipping_group_lines_group FOREIGN KEY (shipping_group_id)
                    REFERENCES "order".order_shipping_groups (shipping_group_id) ON DELETE CASCADE,
                CONSTRAINT fk_order_shipping_group_lines_order_line FOREIGN KEY (order_line_id)
                    REFERENCES "order".order_lines (line_id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ix_order_shipping_group_lines_group
                ON "order".order_shipping_group_lines (shipping_group_id);

            CREATE UNIQUE INDEX IF NOT EXISTS ux_order_shipping_group_lines_order_line
                ON "order".order_shipping_group_lines (order_line_id);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS "order".order_shipping_group_lines;
            DROP TABLE IF EXISTS "order".order_shipping_groups;
            """);
    }
}
