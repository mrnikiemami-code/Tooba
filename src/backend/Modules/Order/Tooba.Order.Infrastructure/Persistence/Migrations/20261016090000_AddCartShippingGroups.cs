using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-SHIPPING-ONE-LEVEL-TABS-126 — گروه‌های ارسال چندگانهٔ پیش‌نویس سبد (Order-owned).
/// یک گروه = یک فروشنده + یک روش متعارف؛ هر خط سبد دقیقاً در یک گروه.
/// جدول‌ها فقط Additive هستند و مسیر تکردوشی قدیمی دست‌نخورده می‌ماند.
/// </summary>
[DbContext(typeof(OrderDbContext))]
[Migration("20261016090000_AddCartShippingGroups")]
public partial class AddCartShippingGroups : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS "order".cart_shipping_groups
            (
                shipping_group_id uuid NOT NULL,
                cart_id uuid NOT NULL,
                seller_party_id uuid NOT NULL,
                method_code character varying(64) NOT NULL,
                service_code character varying(64) NULL,
                option_code character varying(64) NULL,
                method_label character varying(128) NOT NULL,
                origin_context character varying(64) NULL,
                shipping_amount numeric(18,2) NOT NULL,
                currency character varying(3) NOT NULL,
                minimum_delivery_date date NOT NULL,
                selected_delivery_date date NULL,
                selected_delivery_time_window character varying(32) NULL,
                lead_days integer NOT NULL,
                state character varying(16) NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_cart_shipping_groups PRIMARY KEY (shipping_group_id),
                CONSTRAINT fk_cart_shipping_groups_draft FOREIGN KEY (cart_id)
                    REFERENCES "order".cart_shipping_drafts (cart_id) ON DELETE CASCADE,
                CONSTRAINT ck_cart_shipping_groups_amount CHECK (shipping_amount >= 0),
                CONSTRAINT ck_cart_shipping_groups_currency CHECK (char_length(currency) = 3)
            );

            CREATE INDEX IF NOT EXISTS ix_cart_shipping_groups_cart
                ON "order".cart_shipping_groups (cart_id);

            CREATE UNIQUE INDEX IF NOT EXISTS ux_cart_shipping_groups_cart_seller_method
                ON "order".cart_shipping_groups (cart_id, seller_party_id, method_code);

            CREATE TABLE IF NOT EXISTS "order".cart_shipping_group_lines
            (
                shipping_group_id uuid NOT NULL,
                cart_line_id uuid NOT NULL,
                CONSTRAINT pk_cart_shipping_group_lines PRIMARY KEY (shipping_group_id, cart_line_id),
                CONSTRAINT fk_cart_shipping_group_lines_group FOREIGN KEY (shipping_group_id)
                    REFERENCES "order".cart_shipping_groups (shipping_group_id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS ix_cart_shipping_group_lines_group
                ON "order".cart_shipping_group_lines (shipping_group_id);

            CREATE UNIQUE INDEX IF NOT EXISTS ux_cart_shipping_group_lines_cart_line
                ON "order".cart_shipping_group_lines (cart_line_id);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS "order".cart_shipping_group_lines;
            DROP TABLE IF EXISTS "order".cart_shipping_groups;
            """);
    }
}
