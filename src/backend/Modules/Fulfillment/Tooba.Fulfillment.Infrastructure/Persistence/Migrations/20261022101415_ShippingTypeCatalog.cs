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
                    ShippingTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipping_types", x => x.ShippingTypeId);
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
                    ShippingTypeTranslationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShippingTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    LanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipping_type_translations", x => x.ShippingTypeTranslationId);
                    table.CheckConstraint(
                        "ck_shipping_type_translations_name_nonblank",
                        "char_length(name) BETWEEN 1 AND 128 AND name = btrim(name)");
                    table.CheckConstraint(
                        "ck_shipping_type_translations_description_nonblank",
                        "description IS NULL OR (char_length(description) BETWEEN 1 AND 512"
                        + " AND description = btrim(description))");
                    table.ForeignKey(
                        name: "FK_shipping_type_translations_shipping_types_ShippingTypeId",
                        column: x => x.ShippingTypeId,
                        principalSchema: "fulfillment",
                        principalTable: "shipping_types",
                        principalColumn: "ShippingTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shipping_types_code",
                schema: "fulfillment",
                table: "shipping_types",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_shipping_types_active_sort",
                schema: "fulfillment",
                table: "shipping_types",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "ix_shipping_type_translations_type_language",
                schema: "fulfillment",
                table: "shipping_type_translations",
                columns: new[] { "ShippingTypeId", "LanguageId" },
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
