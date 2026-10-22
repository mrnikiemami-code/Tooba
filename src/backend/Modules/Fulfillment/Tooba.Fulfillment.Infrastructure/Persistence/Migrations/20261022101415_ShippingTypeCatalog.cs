using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196 — کاتالوگ نوع ارسال عملیاتی.
    /// </summary>
    /// <remarks>
    /// <para>
    /// فقط دو جدول تازهٔ <c>fulfillment.shipping_types</c> و
    /// <c>fulfillment.shipping_type_translations</c> ساخته می‌شود. این مهاجرت additive است: هیچ جدول
    /// موجود، ستون legacy، قید، شاخص یا ردیف داده‌ای تغییر نمی‌کند و هیچ backfill/seed ندارد.
    /// </para>
    /// <para>
    /// TB-196-R7: نام ستون‌ها و شناسهٔ قیدها با مدل/provider متعارف (snake_case،
    /// <c>ToobaNpgsql.ConfigureModuleContext</c>) هم‌تراز شد. پیش‌تر همین مهاجرت ستون‌های PascalCase
    /// («Code» ...) می‌ساخت در حالی که متن CHECK و مدل snake_case بودند و اجرای واقعی PostgreSQL با
    /// SQLSTATE 42703 شکست می‌خورد. این اصلاح فقط همین مهاجرت پذیرفته‌نشدهٔ پایانی است؛ هیچ مهاجرت قدیمی
    /// تغییر نمی‌کند.
    /// </para>
    /// <para>
    /// قیدهای CHECK آینهٔ گاردهای دامنه‌اند تا ردیفی که دامنه رد می‌کند از SQL خام هم ذخیره نشود. هیچ FK
    /// به Localization یا Shipping ساخته نمی‌شود؛ تنها FK، cascade داخلی خودِ همین دو جدول است.
    /// </para>
    /// </remarks>
    public partial class ShippingTypeCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shipping_types",
                schema: "fulfillment",
                columns: table => new
                {
                    shipping_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    revision = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipping_types", x => x.shipping_type_id);
                    table.CheckConstraint(
                        "ck_shipping_types_code_canonical",
                        "char_length(code) BETWEEN 1 AND 64 AND code = btrim(code) AND code ~ '^[a-z][a-z0-9_]*$'");
                    table.CheckConstraint(
                        "ck_shipping_types_sort_order_range",
                        "sort_order >= 0 AND sort_order <= 1000000");
                    table.CheckConstraint("ck_shipping_types_revision_positive", "revision > 0");
                });

            migrationBuilder.CreateTable(
                name: "shipping_type_translations",
                schema: "fulfillment",
                columns: table => new
                {
                    shipping_type_translation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shipping_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipping_type_translations", x => x.shipping_type_translation_id);
                    table.CheckConstraint(
                        "ck_shipping_type_translations_name_nonblank",
                        "char_length(name) BETWEEN 1 AND 128 AND name = btrim(name)");
                    table.CheckConstraint(
                        "ck_shipping_type_translations_description_nonblank",
                        "description IS NULL OR (char_length(description) BETWEEN 1 AND 512"
                        + " AND description = btrim(description))");
                    table.ForeignKey(
                        name: "fk_shipping_type_translations_shipping_types_shipping_type_id",
                        column: x => x.shipping_type_id,
                        principalSchema: "fulfillment",
                        principalTable: "shipping_types",
                        principalColumn: "shipping_type_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shipping_types_code",
                schema: "fulfillment",
                table: "shipping_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shipping_types_active_sort",
                schema: "fulfillment",
                table: "shipping_types",
                columns: new[] { "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_shipping_type_translations_type_language",
                schema: "fulfillment",
                table: "shipping_type_translations",
                columns: new[] { "shipping_type_id", "language_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shipping_type_translations",
                schema: "fulfillment");

            migrationBuilder.DropTable(
                name: "shipping_types",
                schema: "fulfillment");
        }
    }
}
