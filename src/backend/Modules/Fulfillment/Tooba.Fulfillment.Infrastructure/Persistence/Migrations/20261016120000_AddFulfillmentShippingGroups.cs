using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Fulfillment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FulfillmentDbContext))]
    [Migration("20261016120000_AddFulfillmentShippingGroups")]
    public partial class AddFulfillmentShippingGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                -- گروه ارسال پذیرفته‌شدهٔ مالک Fulfillment. هویت پایدار از سفارش پذیرفته‌شده می‌آید؛
                -- هیچ نرخ، تخصیص carrier یا مبدأ جعلی اینجا ساخته نمی‌شود.
                CREATE TABLE IF NOT EXISTS fulfillment.shipping_groups
                (
                    shipping_group_id uuid NOT NULL,
                    fulfillment_id uuid NOT NULL,
                    seller_order_id uuid NOT NULL,
                    seller_party_id uuid NOT NULL,
                    method_code character varying(64) NOT NULL,
                    service_code character varying(64) NULL,
                    option_code character varying(64) NULL,
                    method_label character varying(128) NOT NULL,
                    origin_context character varying(256) NULL,
                    minimum_delivery_date date NOT NULL,
                    requested_delivery_date date NULL,
                    requested_delivery_time_window character varying(64) NULL,
                    lead_days integer NOT NULL,
                    CONSTRAINT pk_shipping_groups PRIMARY KEY (shipping_group_id)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_shipping_groups_fulfillment_group
                    ON fulfillment.shipping_groups (fulfillment_id, shipping_group_id);

                CREATE INDEX IF NOT EXISTS ix_shipping_groups_fulfillment_id
                    ON fulfillment.shipping_groups (fulfillment_id);

                -- عضویت خط: یک خط سفارش دقیقاً به یک گروه. ردیف‌های قدیمی NULL می‌مانند و خوانده می‌شوند.
                ALTER TABLE fulfillment.items
                    ADD COLUMN IF NOT EXISTS shipping_group_id uuid NULL;

                CREATE INDEX IF NOT EXISTS ix_items_shipping_group_id
                    ON fulfillment.items (shipping_group_id)
                    WHERE shipping_group_id IS NOT NULL;

                -- محموله هویت گروه واقعی خود را نگه می‌دارد تا dispatch/cancel/ردیابی گروه را جابه‌جا نکند.
                ALTER TABLE fulfillment.shipments
                    ADD COLUMN IF NOT EXISTS shipping_group_id uuid NULL;

                CREATE INDEX IF NOT EXISTS ix_shipments_shipping_group_id
                    ON fulfillment.shipments (shipping_group_id)
                    WHERE shipping_group_id IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS fulfillment.ix_shipments_shipping_group_id;
                ALTER TABLE fulfillment.shipments DROP COLUMN IF EXISTS shipping_group_id;
                DROP INDEX IF EXISTS fulfillment.ix_items_shipping_group_id;
                ALTER TABLE fulfillment.items DROP COLUMN IF EXISTS shipping_group_id;
                DROP TABLE IF EXISTS fulfillment.shipping_groups;
                """);
        }
    }
}
