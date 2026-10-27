using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-SHIPPING-COMPACT-GROUP-ORDER-EVIDENCE-231 (corrected in TB-231-R1) — شاهد فشردهٔ پذیرفته‌شدهٔ هر گروه.
/// </summary>
/// <remarks>
/// <para>
/// فقط ستون‌های nullable روی همان جدول‌های موجود (پیش‌نویس و سفارش) اضافه می‌شوند؛ هیچ جدول موازی،
/// FK بین‌ماژولی یا کپی snapshot ساخته نمی‌شود و گروه‌های قدیمی همه null می‌مانند.
/// </para>
/// <para>
/// دقت‌ها بی‌افت‌اند: بازبینی/نسخهٔ انتخاب bigint (هم‌دقت منبع long)، ساعت‌های دریافت به‌صورت تیک
/// (bigint، بازهٔ کامل TimeOnly) نه time/timestamp، و مبالغ numeric(18,2) که سازنده از قبل دقیقاً
/// قابل‌نمایش بودنشان را تضمین می‌کند.
/// </para>
/// <para>
/// این مهاجرت عمداً ADD COLUMN ساده (بدون IF NOT EXISTS) و CHECKهای صریح دارد: اگر شیئی با همین نام و
/// نوع ناسازگار از قبل باشد اجرا شکست می‌خورد و هیچ schema ناشناخته‌ای پذیرفته نمی‌شود.
/// </para>
/// </remarks>
[DbContext(typeof(OrderDbContext))]
[Migration("20261027150000_AddShippingGroupAcceptedEvidence")]
public partial class AddShippingGroupAcceptedEvidence : Migration
{
    private static readonly string[] EvidenceColumns =
    [
        "accepted_snapshot_id uuid NULL",
        "accepted_draft_id uuid NULL",
        "accepted_source_revision bigint NULL",
        "accepted_format_version integer NULL",
        "accepted_selection_version bigint NULL",
        "accepted_owner_kind integer NULL",
        "accepted_owner_tenant_key character varying(64) NULL",
        "accepted_owner_seller_party_id uuid NULL",
        "accepted_area_group_id uuid NULL",
        "accepted_rate_row_id uuid NULL",
        "accepted_shipping_type_id uuid NULL",
        "accepted_freight_amount numeric(18,2) NULL",
        "accepted_packaging_amount numeric(18,2) NULL",
        "accepted_currency character varying(3) NULL",
        "accepted_payable_at_checkout_amount numeric(18,2) NULL",
        "accepted_carrier_collect_amount numeric(18,2) NULL",
        "accepted_receiving_date date NULL",
        "accepted_receiving_open_ticks bigint NULL",
        "accepted_receiving_close_ticks bigint NULL",
        "accepted_receiving_time_zone character varying(64) NULL",
        "accepted_minimum_delivery_days integer NULL",
        "accepted_maximum_delivery_days integer NULL",
    ];

    /// <summary>ستون‌هایی که در شاهد موجود باید غیرnull باشند (بقیه هنگام وجود اختیاری‌اند).</summary>
    private static readonly string[] RequiredWhenPresent =
    [
        "accepted_snapshot_id",
        "accepted_draft_id",
        "accepted_source_revision",
        "accepted_format_version",
        "accepted_selection_version",
        "accepted_owner_kind",
        "accepted_area_group_id",
        "accepted_rate_row_id",
        "accepted_freight_amount",
        "accepted_packaging_amount",
        "accepted_currency",
        "accepted_payable_at_checkout_amount",
        "accepted_carrier_collect_amount",
        "accepted_receiving_date",
        "accepted_receiving_open_ticks",
        "accepted_receiving_close_ticks",
        "accepted_receiving_time_zone",
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "cart_shipping_groups", "order_shipping_groups" })
        {
            foreach (var column in EvidenceColumns)
            {
                migrationBuilder.Sql($"ALTER TABLE \"order\".{table} ADD COLUMN {column};");
            }

            // همه‌یا‌هیچ: یا کل شاهد غایب است، یا همهٔ ستون‌های لازم پر شده‌اند؛ ردیف نیمه‌کامل ذخیره نمی‌شود.
            var allNull = string.Join(
                " AND ",
                EvidenceColumns.Select(column => $"{column.Split(' ')[0]} IS NULL"));
            var allPresent = string.Join(
                " AND ",
                RequiredWhenPresent.Select(column => $"{column} IS NOT NULL"));

            migrationBuilder.Sql(
                $"ALTER TABLE \"order\".{table} ADD CONSTRAINT ck_{table}_accepted_evidence_complete "
                + $"CHECK (({allNull}) OR ({allPresent}));");

            // شکل پایه: بازهٔ مثبتِ همان‌روز، ارز سه‌حرفی ASCII، مبالغ نامنفی و متوازن، مالک دقیقاً یکی از سه شکل مجاز.
            migrationBuilder.Sql(BuildShapeConstraint(table));
        }

        // ارجاع منطقی snapshot برای ردیابی تاریخی سفارش‌ها؛ یک ایندکس کوچک و بدون پوشش گسترده.
        migrationBuilder.Sql(
            """
            CREATE INDEX ix_order_shipping_groups_accepted_snapshot_id
                ON "order".order_shipping_groups (accepted_snapshot_id);
            """);
    }

    /// <summary>
    /// CHECKهای شکل واقعیِ ذخیره‌سازی برای یک جدول؛ همهٔ شاخه‌ها FALSE/TRUE می‌دهند، نه UNKNOWN.
    /// </summary>
    /// <remarks>
    /// نکتهٔ سه‌مقداری SQL: یک CHECK با نتیجهٔ UNKNOWN پذیرفته می‌شود. بنابراین جفت نیمه‌کاملِ
    /// روزهای تحویل با <c>IS NOT NULL</c> صریح پوشانده می‌شود تا جفت ناقص «رد» شود، نه «نامعلوم».
    /// مقادیر UUID خالی، tenant خالی/غیر‌canonical و ارز غیر‌ASCII نیز همین‌جا رد می‌شوند.
    /// </remarks>
    private static string BuildShapeConstraint(string table) =>
        """
        ALTER TABLE "order".__TABLE__ ADD CONSTRAINT ck___TABLE___accepted_evidence_shape
        CHECK (
            accepted_snapshot_id IS NULL
            OR (
                accepted_snapshot_id <> '00000000-0000-0000-0000-000000000000'::uuid
                AND accepted_draft_id <> '00000000-0000-0000-0000-000000000000'::uuid
                AND accepted_area_group_id <> '00000000-0000-0000-0000-000000000000'::uuid
                AND accepted_rate_row_id <> '00000000-0000-0000-0000-000000000000'::uuid
                AND (
                    accepted_shipping_type_id IS NULL
                    OR accepted_shipping_type_id <> '00000000-0000-0000-0000-000000000000'::uuid
                )
                AND accepted_source_revision > 0
                AND accepted_format_version = 1
                AND accepted_selection_version > 0
                AND accepted_owner_kind IN (0, 1, 2)
                AND accepted_currency ~ '^[A-Z]{3}$'
                AND accepted_freight_amount >= 0
                AND accepted_packaging_amount >= 0
                AND accepted_payable_at_checkout_amount >= 0
                AND accepted_carrier_collect_amount >= 0
                AND accepted_freight_amount + accepted_packaging_amount
                    = accepted_payable_at_checkout_amount + accepted_carrier_collect_amount
                AND accepted_receiving_open_ticks >= 0
                AND accepted_receiving_open_ticks < accepted_receiving_close_ticks
                AND accepted_receiving_close_ticks <= 863999999999
                AND char_length(accepted_receiving_time_zone) > 0
                AND (
                    (
                        accepted_minimum_delivery_days IS NULL
                        AND accepted_maximum_delivery_days IS NULL
                    )
                    OR (
                        accepted_minimum_delivery_days IS NOT NULL
                        AND accepted_maximum_delivery_days IS NOT NULL
                        AND accepted_minimum_delivery_days >= 0
                        AND accepted_maximum_delivery_days >= accepted_minimum_delivery_days
                        AND accepted_maximum_delivery_days <= 365
                    )
                )
                AND (
                    (
                        accepted_owner_kind = 2
                        AND accepted_owner_seller_party_id IS NOT NULL
                        AND accepted_owner_seller_party_id <> '00000000-0000-0000-0000-000000000000'::uuid
                        AND accepted_owner_tenant_key IS NULL
                    )
                    OR (
                        accepted_owner_kind = 0
                        AND accepted_owner_tenant_key IS NOT NULL
                        AND btrim(accepted_owner_tenant_key) <> ''
                        AND accepted_owner_tenant_key = btrim(accepted_owner_tenant_key)
                        AND accepted_owner_seller_party_id IS NULL
                    )
                    OR (
                        accepted_owner_kind = 1
                        AND accepted_owner_tenant_key IS NULL
                        AND accepted_owner_seller_party_id IS NULL
                    )
                )
            )
        );
        """
        .Replace("__TABLE__", table);

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP INDEX IF EXISTS \"order\".ix_order_shipping_groups_accepted_snapshot_id;");

        foreach (var table in new[] { "order_shipping_groups", "cart_shipping_groups" })
        {
            migrationBuilder.Sql(
                $"ALTER TABLE \"order\".{table} DROP CONSTRAINT IF EXISTS ck_{table}_accepted_evidence_shape;");
            migrationBuilder.Sql(
                $"ALTER TABLE \"order\".{table} DROP CONSTRAINT IF EXISTS ck_{table}_accepted_evidence_complete;");

            foreach (var column in EvidenceColumns)
            {
                migrationBuilder.Sql(
                    $"ALTER TABLE \"order\".{table} DROP COLUMN IF EXISTS {column.Split(' ')[0]};");
            }
        }
    }
}
