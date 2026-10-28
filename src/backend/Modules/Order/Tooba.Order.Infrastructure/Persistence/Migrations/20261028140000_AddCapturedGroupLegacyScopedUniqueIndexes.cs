using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>
/// TB-SHIPPING-MARKETPLACE-CONNECTED-DELIVERY-240-R3: یکتایی «یک گروه برای هر فروشنده+روش» فقط برای
/// گروه‌های <b>قدیمی</b> (بدون شاهد تأییدشده) حفظ می‌شود.
/// </summary>
/// <remarks>
/// <para>
/// در Marketplace یک فروشندهٔ واقعی می‌تواند هم‌زمان یک تعهد Platform-owned و یک تعهد Seller-owned روی
/// <b>یک سبد</b> و با <b>همان روش یک‌سطحی کاتالوگ</b> داشته باشد (خطوط جدا). ایندکس یکتای بی‌قید قبلی این
/// حالت قانونی را رد می‌کرد. این مهاجرت <b>هیچ ستون/جدول جدیدی</b> نمی‌سازد و فقط هر دو ایندکس موجود را
/// با یک ایندکس یکتای <b>جزئی</b> روی همان کلیدها جایگزین می‌کند که predicate آن
/// <c>accepted_snapshot_id IS NULL</c> است: ردیف‌های قدیمی همان محدودیت قبلی را دارند و ردیف‌های دارای
/// شاهد کامل تأییدشده می‌توانند کنار هم بمانند.
/// </para>
/// <para>
/// هویت گروه، شاهد کامل مالک (Store/Marketplace/Seller) و یکتایی دقیق عضویت خطوط دست‌نخورده می‌مانند؛
/// هیچ ستون مالک تکراری، پسوند روش، کپی JSON یا جدول تازه ساخته نمی‌شود.
/// </para>
/// </remarks>
[DbContext(typeof(OrderDbContext))]
[Migration("20261028140000_AddCapturedGroupLegacyScopedUniqueIndexes")]
public sealed class AddCapturedGroupLegacyScopedUniqueIndexes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // DROP صریح شئ شناخته‌شده؛ هیچ IF NOT EXISTS/قبول ضمنی شئ ناسازگار انجام نمی‌شود.
        migrationBuilder.Sql(
            """DROP INDEX "order".ux_cart_shipping_groups_cart_seller_method;""");

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX ux_cart_shipping_groups_cart_seller_method
                ON "order".cart_shipping_groups (cart_id, seller_party_id, method_code)
                WHERE accepted_snapshot_id IS NULL;
            """);

        migrationBuilder.Sql(
            """DROP INDEX "order".ux_order_shipping_groups_checkout_seller_method;""");

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX ux_order_shipping_groups_checkout_seller_method
                ON "order".order_shipping_groups (checkout_id, seller_party_id, method_code)
                WHERE accepted_snapshot_id IS NULL;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // بازگشت فقط وقتی ممکن است که هیچ گروه تأییدشدهٔ تکراری وجود نداشته باشد؛ در غیر این صورت
        // CREATE UNIQUE شکست می‌خورد و تراکنش کامل برمی‌گردد. هیچ گروهی حذف/ادغام نمی‌شود.
        migrationBuilder.Sql(
            """DROP INDEX "order".ux_order_shipping_groups_checkout_seller_method;""");

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX ux_order_shipping_groups_checkout_seller_method
                ON "order".order_shipping_groups (checkout_id, seller_party_id, method_code);
            """);

        migrationBuilder.Sql(
            """DROP INDEX "order".ux_cart_shipping_groups_cart_seller_method;""");

        migrationBuilder.Sql(
            """
            CREATE UNIQUE INDEX ux_cart_shipping_groups_cart_seller_method
                ON "order".cart_shipping_groups (cart_id, seller_party_id, method_code);
            """);
    }
}
