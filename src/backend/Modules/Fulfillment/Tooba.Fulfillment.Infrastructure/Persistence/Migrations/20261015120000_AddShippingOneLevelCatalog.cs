using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Fulfillment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FulfillmentDbContext))]
    [Migration("20261015120000_AddShippingOneLevelCatalog")]
    public partial class AddShippingOneLevelCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS fulfillment.shipping_methods
                (
                    shipping_method_id uuid NOT NULL,
                    method_code character varying(32) NOT NULL,
                    provider_kind character varying(64) NOT NULL,
                    legacy_service_code character varying(64) NOT NULL,
                    legacy_option_code character varying(64) NULL,
                    sort_order integer NOT NULL,
                    is_active boolean NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_shipping_methods PRIMARY KEY (shipping_method_id),
                    CONSTRAINT ck_shipping_methods_code_canonical CHECK
                    (
                        length(method_code) BETWEEN 1 AND 32
                        AND method_code = lower(method_code)
                        AND method_code !~ '[^a-z0-9_:]'
                    ),
                    CONSTRAINT ck_shipping_methods_option_code_canonical CHECK
                    (
                        legacy_option_code IS NULL
                        OR (length(btrim(legacy_option_code)) BETWEEN 1 AND 64
                            AND legacy_option_code = lower(legacy_option_code))
                    )
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_shipping_methods_method_code
                    ON fulfillment.shipping_methods (method_code);

                -- پیوند قدیمی گزینه‌دار یکتا است؛ ردیف سرویس بدون گزینه (legacy_option_code IS NULL)
                -- عمداً در این یکتایی نیست تا نگاشت fallback سطح سرویس به روش دوم تبدیل نشود.
                CREATE UNIQUE INDEX IF NOT EXISTS ux_shipping_methods_legacy_binding
                    ON fulfillment.shipping_methods (legacy_service_code, legacy_option_code)
                    WHERE legacy_option_code IS NOT NULL;

                CREATE INDEX IF NOT EXISTS ix_shipping_methods_legacy_service_code
                    ON fulfillment.shipping_methods (legacy_service_code);

                CREATE TABLE IF NOT EXISTS fulfillment.shipping_method_translations
                (
                    translation_id uuid NOT NULL,
                    shipping_method_id uuid NOT NULL,
                    language_id uuid NOT NULL,
                    name character varying(128) NOT NULL,
                    description character varying(512) NULL,
                    CONSTRAINT pk_shipping_method_translations PRIMARY KEY (translation_id),
                    -- نگهبان یکپارچگی مرجع برای جدول تازهٔ ترجمه: بدون آن، حذف یک روش، ترجمه‌های یتیم
                    -- باقی می‌گذارد. این محدودیت فقط روی جدول تازه اعمال می‌شود و به جدول‌های موجود
                    -- ماژول (که هیچ FK سطح دیتابیس ندارند) دست نمی‌زند.
                    CONSTRAINT fk_shipping_method_translations_method FOREIGN KEY (shipping_method_id)
                        REFERENCES fulfillment.shipping_methods (shipping_method_id) ON DELETE CASCADE
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_shipping_method_translations_method_lang
                    ON fulfillment.shipping_method_translations (shipping_method_id, language_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS fulfillment.shipping_method_translations;
                DROP TABLE IF EXISTS fulfillment.shipping_methods;
                """);
        }
    }
}
