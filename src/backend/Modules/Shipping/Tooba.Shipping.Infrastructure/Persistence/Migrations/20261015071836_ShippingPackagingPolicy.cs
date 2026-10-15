using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ShippingPackagingPolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "packaging_policies",
                schema: "shipping",
                columns: table => new
                {
                    packaging_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    option_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    administrative_area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    city_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    fee_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    freight_payer_mode = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_packaging_policies", x => x.packaging_policy_id);
                    table.CheckConstraint("ck_packaging_policies_currency_code_canonical", "char_length(currency_code) = 3 AND currency_code = upper(currency_code)");
                    table.CheckConstraint("ck_packaging_policies_fee_amount_non_negative", "fee_amount >= 0");
                    table.CheckConstraint("ck_packaging_policies_fee_amount_within_max", "fee_amount <= 1000000000");
                    table.CheckConstraint("ck_packaging_policies_freight_payer_mode_known", "freight_payer_mode IN (0, 1)");
                    table.CheckConstraint("ck_packaging_policies_option_code_canonical", "option_code IS NULL OR (char_length(option_code) > 0 AND option_code = btrim(option_code))");
                    table.CheckConstraint("ck_packaging_policies_scope_coherent", "service_area_id IS NULL OR city_id IS NOT NULL");
                    table.CheckConstraint("ck_packaging_policies_service_code_canonical", "char_length(service_code) > 0 AND service_code = btrim(service_code)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_packaging_policies_service_active",
                schema: "shipping",
                table: "packaging_policies",
                columns: new[] { "service_code", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ux_packaging_policies_service_option_scope",
                schema: "shipping",
                table: "packaging_policies",
                columns: new[] { "service_code", "option_code", "country_id", "administrative_area_id", "city_id", "service_area_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "packaging_policies",
                schema: "shipping");
        }
    }
}
