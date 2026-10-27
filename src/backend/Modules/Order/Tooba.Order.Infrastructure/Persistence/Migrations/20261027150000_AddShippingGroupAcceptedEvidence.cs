using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231 — شاهد فشردهٔ پذیرفته‌شدهٔ هر گروه ارسال.
/// فقط ستون‌های nullable روی همان جدول‌های موجود (پیش‌نویس و سفارش) اضافه می‌شوند؛ هیچ جدول
/// موازی، FK بین‌ماژولی یا کپی snapshot ساخته نمی‌شود و گروه‌های قدیمی همه null می‌مانند.
/// هر گروه فقط ارجاع snapshot/Draft/بازبینی و مبالغ/دریافت نهایی را نگه می‌دارد، نه JSON کامل
/// authoring؛ پس حجم ردیف به تعداد خط سبد/سفارش وابسته نیست.
/// </summary>
[DbContext(typeof(OrderDbContext))]
[Migration("20261027150000_AddShippingGroupAcceptedEvidence")]
public partial class AddShippingGroupAcceptedEvidence : Migration
{
    private static readonly string[] EvidenceColumns =
    [
        "accepted_snapshot_id uuid NULL",
        "accepted_draft_id uuid NULL",
        "accepted_source_revision integer NULL",
        "accepted_format_version integer NULL",
        "accepted_selection_version integer NULL",
        "accepted_responsibility integer NULL",
        "accepted_area_group_id uuid NULL",
        "accepted_rate_row_id uuid NULL",
        "accepted_shipping_type_id uuid NULL",
        "accepted_freight_amount numeric(18,2) NULL",
        "accepted_packaging_amount numeric(18,2) NULL",
        "accepted_currency character varying(3) NULL",
        "accepted_payable_at_checkout_amount numeric(18,2) NULL",
        "accepted_carrier_collect_amount numeric(18,2) NULL",
        "accepted_receiving_start_date date NULL",
        "accepted_receiving_end_date date NULL",
        "accepted_receiving_time_zone character varying(64) NULL",
        "accepted_minimum_delivery_days integer NULL",
        "accepted_maximum_delivery_days integer NULL",
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "cart_shipping_groups", "order_shipping_groups" })
        {
            foreach (var column in EvidenceColumns)
            {
                migrationBuilder.Sql(
                    $"ALTER TABLE \"order\".{table} ADD COLUMN IF NOT EXISTS {column};");
            }
        }

        // ارجاع منطقی snapshot برای ردیابی تاریخی سفارش‌ها؛ یک ایندکس کوچک و بدون پوشش گسترده.
        migrationBuilder.Sql(
            """
            CREATE INDEX IF NOT EXISTS ix_order_shipping_groups_accepted_snapshot_id
                ON "order".order_shipping_groups (accepted_snapshot_id);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP INDEX IF EXISTS \"order\".ix_order_shipping_groups_accepted_snapshot_id;");

        foreach (var table in new[] { "order_shipping_groups", "cart_shipping_groups" })
        {
            foreach (var column in EvidenceColumns)
            {
                var name = column.Split(' ')[0];
                migrationBuilder.Sql(
                    $"ALTER TABLE \"order\".{table} DROP COLUMN IF EXISTS {name};");
            }
        }
    }
}
