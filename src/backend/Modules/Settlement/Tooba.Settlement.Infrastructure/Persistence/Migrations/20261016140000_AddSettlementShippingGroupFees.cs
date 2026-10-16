using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Settlement.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Settlement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(SettlementDbContext))]
    [Migration("20261016140000_AddSettlementShippingGroupFees")]
    public partial class AddSettlementShippingGroupFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                -- حساب‌داری هزینهٔ ارسال per-group مالک Settlement. کلید پایدار shipping_group_id و هویت
                -- پرداخت/سفارش/فروشنده ذخیره می‌شود. فقط charge مشتری (accepted quote و مبلغ پرداخت‌شده)
                -- KNOWN است؛ سهم carrier/فروشنده/فروشگاه/تخفیف/مالیات/funder UNSPECIFIED و NULL می‌ماند و
                -- هرگز صفر پر نمی‌شود. این سطر وارد ledger فروشنده یا payout نمی‌شود.
                CREATE TABLE IF NOT EXISTS settlement.shipping_group_fees
                (
                    shipping_group_fee_id uuid NOT NULL,
                    payment_id uuid NOT NULL,
                    shipping_group_id uuid NOT NULL,
                    checkout_id uuid NOT NULL,
                    seller_order_id uuid NOT NULL,
                    seller_party_id uuid NOT NULL,
                    method_code character varying(64) NOT NULL,
                    service_code character varying(64) NULL,
                    option_code character varying(64) NULL,
                    accepted_quote_version character varying(32) NOT NULL,
                    customer_charge_amount numeric(19,4) NOT NULL,
                    currency character varying(8) NOT NULL,
                    allocation_state character varying(32) NOT NULL,
                    allocation_policy_version character varying(64) NULL,
                    allocation_policy_reference character varying(128) NULL,
                    carrier_payable_amount numeric(19,4) NULL,
                    seller_share_amount numeric(19,4) NULL,
                    marketplace_share_amount numeric(19,4) NULL,
                    shipping_discount_amount numeric(19,4) NULL,
                    shipping_tax_amount numeric(19,4) NULL,
                    funder_adjustment_amount numeric(19,4) NULL,
                    funder_reference character varying(128) NULL,
                    record_kind character varying(16) NOT NULL,
                    reversal_of_shipping_group_fee_id uuid NULL,
                    idempotency_key character varying(160) NOT NULL,
                    recorded_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_shipping_group_fees PRIMARY KEY (shipping_group_fee_id)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_shipping_group_fees_idempotency_key
                    ON settlement.shipping_group_fees (idempotency_key);

                -- یک charge پایدار برای هر (payment, group)؛ reversalها مستثنا هستند.
                CREATE UNIQUE INDEX IF NOT EXISTS ix_shipping_group_fees_payment_group_charge
                    ON settlement.shipping_group_fees (payment_id, shipping_group_id)
                    WHERE reversal_of_shipping_group_fee_id IS NULL;

                CREATE INDEX IF NOT EXISTS ix_shipping_group_fees_payment_group
                    ON settlement.shipping_group_fees (payment_id, shipping_group_id);

                CREATE INDEX IF NOT EXISTS ix_shipping_group_fees_shipping_group_id
                    ON settlement.shipping_group_fees (shipping_group_id);

                CREATE INDEX IF NOT EXISTS ix_shipping_group_fees_seller_party_id
                    ON settlement.shipping_group_fees (seller_party_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS settlement.shipping_group_fees;
                """);
        }
    }
}
