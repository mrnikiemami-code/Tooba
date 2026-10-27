using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-SHIPPING-CAPTURED-COUPON-CONTEXT-PARITY-236: توکن تأییدشدهٔ قصد کوپن روی ریشهٔ انتخاب و مبلغ
/// تأییدشدهٔ کالای هر گروه پیش‌نویس.
/// </summary>
/// <remarks>
/// <para>
/// این مهاجرت فقط <b>افزایشی</b> است: یک ستون nullable روی <c>order.cart_shipping_drafts</c> و یک
/// ستون nullable روی <c>order.cart_shipping_groups</c>. هیچ ستون/جدول/ایندکس قبلی تغییر نمی‌کند،
/// هیچ backfill انجام نمی‌شود (ردیف‌های قدیمی NULL می‌مانند و همین NULL یعنی «تأیید نشده»)، هیچ
/// JSON/JSONB و هیچ snapshot/تاریخچهٔ کوپن ساخته نمی‌شود. قیمت و تاریخچهٔ مالی همان ستون‌های موجود
/// خط/گروه سفارش می‌مانند.
/// </para>
/// <para>
/// <b>WRITTEN / NOT APPLIED.</b> این مهاجرت همراه ۲۳۱/۲۳۲ فقط برای اثبات روی پایگاه‌دادهٔ دورانداختنی
/// همین تسک اجرا می‌شود و به <c>tooba_alpha</c>/store-alpha اعمال نمی‌شود.
/// </para>
/// </remarks>
[DbContext(typeof(OrderDbContext))]
[Migration("20261027230000_AddCapturedSelectionCouponContext")]
public sealed class AddCapturedSelectionCouponContext : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "order".cart_shipping_drafts
                ADD COLUMN selection_coupon_code text NULL;
            """);

        migrationBuilder.Sql(
            """
            ALTER TABLE "order".cart_shipping_groups
                ADD COLUMN confirmed_goods_amount numeric(18, 2) NULL;
            """);

        // شکل پایه: کد کوپن (وقتی هست) خالی/فاصله‌دار/غیرtrim نیست و مبلغ تأییدشده نامنفی و
        // نمایش‌پذیر در numeric(18,2) است. هیچ سیاست تطبیق/تخفیفی این‌جا تکرار نمی‌شود.
        migrationBuilder.Sql(
            """
            ALTER TABLE "order".cart_shipping_drafts
                ADD CONSTRAINT ck_cart_shipping_drafts_selection_coupon_shape
                CHECK (
                    selection_coupon_code IS NULL
                    OR (btrim(selection_coupon_code) = selection_coupon_code
                        AND selection_coupon_code <> '')
                );
            """);

        migrationBuilder.Sql(
            """
            ALTER TABLE "order".cart_shipping_groups
                ADD CONSTRAINT ck_cart_shipping_groups_confirmed_goods_shape
                CHECK (
                    confirmed_goods_amount IS NULL
                    OR (confirmed_goods_amount >= 0
                        AND confirmed_goods_amount <= 9999999999999999.99)
                );
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "ALTER TABLE \"order\".cart_shipping_groups DROP CONSTRAINT IF EXISTS ck_cart_shipping_groups_confirmed_goods_shape;");
        migrationBuilder.Sql(
            "ALTER TABLE \"order\".cart_shipping_drafts DROP CONSTRAINT IF EXISTS ck_cart_shipping_drafts_selection_coupon_shape;");
        migrationBuilder.Sql(
            "ALTER TABLE \"order\".cart_shipping_groups DROP COLUMN IF EXISTS confirmed_goods_amount;");
        migrationBuilder.Sql(
            "ALTER TABLE \"order\".cart_shipping_drafts DROP COLUMN IF EXISTS selection_coupon_code;");
    }
}
