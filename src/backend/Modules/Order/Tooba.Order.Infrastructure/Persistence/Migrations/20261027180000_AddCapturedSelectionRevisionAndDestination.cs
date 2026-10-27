using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-SHIPPING-CAPTURED-SELECTION-ATOMIC-WRITER-232: توکن CAS اتمیک انتخاب و هویت مقصد تأییدشده.
/// </summary>
/// <remarks>
/// <para>
/// این مهاجرت فقط <b>افزایشی</b> است: پنج ستون nullable/پیش‌فرض‌دار روی همان
/// <c>order.cart_shipping_drafts</c> موجود. هیچ ستون/جدول/ایندکس قبلی تغییر نمی‌کند، هیچ backfill
/// انجام نمی‌شود (ردیف‌های قدیمی <c>selection_revision = 0</c> می‌مانند) و هیچ دادهٔ جغرافیایی
/// JSON‌شده اضافه نمی‌شود — فقط چهار اسکالر شناسه.
/// </para>
/// <para>
/// <b>WRITTEN / NOT APPLIED.</b> این مهاجرت همراه ۲۳۱ فقط برای اثبات روی پایگاه‌دادهٔ دورانداختنی
/// همین تسک اجرا می‌شود و به <c>tooba_alpha</c>/store-alpha اعمال نمی‌شود.
/// </para>
/// </remarks>
[DbContext(typeof(OrderDbContext))]
[Migration("20261027180000_AddCapturedSelectionRevisionAndDestination")]
public sealed class AddCapturedSelectionRevisionAndDestination : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "order".cart_shipping_drafts
                ADD COLUMN selection_revision bigint NOT NULL DEFAULT 0,
                ADD COLUMN destination_country_id uuid NULL,
                ADD COLUMN destination_administrative_area_id uuid NULL,
                ADD COLUMN destination_city_id uuid NULL,
                ADD COLUMN destination_service_area_id uuid NULL;
            """);

        // شکل پایه: نسخهٔ انتخاب نامنفی و هر شناسهٔ مقصد (وقتی هست) غیرتهی است.
        migrationBuilder.Sql(
            """
            ALTER TABLE "order".cart_shipping_drafts
                ADD CONSTRAINT ck_cart_shipping_drafts_captured_selection_shape
                CHECK (
                    selection_revision >= 0
                    AND (destination_country_id IS NULL
                         OR destination_country_id <> '00000000-0000-0000-0000-000000000000'::uuid)
                    AND (destination_administrative_area_id IS NULL
                         OR destination_administrative_area_id <> '00000000-0000-0000-0000-000000000000'::uuid)
                    AND (destination_city_id IS NULL
                         OR destination_city_id <> '00000000-0000-0000-0000-000000000000'::uuid)
                    AND (destination_service_area_id IS NULL
                         OR destination_service_area_id <> '00000000-0000-0000-0000-000000000000'::uuid)
                );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "ALTER TABLE \"order\".cart_shipping_drafts DROP CONSTRAINT IF EXISTS ck_cart_shipping_drafts_captured_selection_shape;");
        migrationBuilder.Sql(
            """
            ALTER TABLE "order".cart_shipping_drafts
                DROP COLUMN IF EXISTS destination_service_area_id,
                DROP COLUMN IF EXISTS destination_city_id,
                DROP COLUMN IF EXISTS destination_administrative_area_id,
                DROP COLUMN IF EXISTS destination_country_id,
                DROP COLUMN IF EXISTS selection_revision;
            """);
    }
}
