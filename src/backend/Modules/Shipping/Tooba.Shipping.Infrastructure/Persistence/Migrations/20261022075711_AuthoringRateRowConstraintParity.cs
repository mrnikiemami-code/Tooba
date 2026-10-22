using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthoringRateRowConstraintParity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_authoring_region_groups_currency_canonical",
                schema: "shipping",
                table: "authoring_region_groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_authoring_rate_rows_amount_within_max",
                schema: "shipping",
                table: "authoring_rate_rows");

            migrationBuilder.DropCheckConstraint(
                name: "ck_authoring_rate_rows_count_interval",
                schema: "shipping",
                table: "authoring_rate_rows");

            migrationBuilder.DropCheckConstraint(
                name: "ck_authoring_rate_rows_merchandise_interval",
                schema: "shipping",
                table: "authoring_rate_rows");

            migrationBuilder.DropCheckConstraint(
                name: "ck_authoring_rate_rows_weight_interval",
                schema: "shipping",
                table: "authoring_rate_rows");

            migrationBuilder.AddCheckConstraint(
                name: "ck_authoring_region_groups_currency_canonical",
                schema: "shipping",
                table: "authoring_region_groups",
                sql: "currency_code IS NULL OR (octet_length(currency_code) = 3 AND currency_code COLLATE \"C\" ~ '^[A-Z]{3}$')");

            migrationBuilder.AddCheckConstraint(
                name: "ck_authoring_rate_rows_amount_within_max",
                schema: "shipping",
                table: "authoring_rate_rows",
                sql: "freight_amount <= 1000000000 AND packaging_amount <= 1000000000 AND (min_merchandise_amount IS NULL OR min_merchandise_amount <= 1000000000) AND (max_merchandise_amount IS NULL OR max_merchandise_amount <= 1000000000)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_authoring_rate_rows_count_interval",
                schema: "shipping",
                table: "authoring_rate_rows",
                sql: "(min_item_count IS NULL AND max_item_count IS NULL) OR (min_item_count IS NOT NULL AND min_item_count >= 1 AND min_item_count <= 1000000 AND (max_item_count IS NULL OR (max_item_count > min_item_count AND max_item_count <= 1000000)))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_authoring_rate_rows_merchandise_interval",
                schema: "shipping",
                table: "authoring_rate_rows",
                sql: "(min_merchandise_amount IS NULL AND max_merchandise_amount IS NULL) OR (min_merchandise_amount IS NOT NULL AND min_merchandise_amount >= 0 AND min_merchandise_amount <= 1000000000 AND (max_merchandise_amount IS NULL OR (max_merchandise_amount > min_merchandise_amount AND max_merchandise_amount <= 1000000000)))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_authoring_rate_rows_weight_interval",
                schema: "shipping",
                table: "authoring_rate_rows",
                sql: "(min_weight_grams IS NULL AND max_weight_grams IS NULL) OR (min_weight_grams IS NOT NULL AND min_weight_grams >= 0 AND min_weight_grams <= 1000000000 AND (max_weight_grams IS NULL OR (max_weight_grams > min_weight_grams AND max_weight_grams <= 1000000000)))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_authoring_region_groups_currency_canonical",
                schema: "shipping",
                table: "authoring_region_groups");

            migrationBuilder.DropCheckConstraint(
                name: "ck_authoring_rate_rows_amount_within_max",
                schema: "shipping",
                table: "authoring_rate_rows");

            migrationBuilder.DropCheckConstraint(
                name: "ck_authoring_rate_rows_count_interval",
                schema: "shipping",
                table: "authoring_rate_rows");

            migrationBuilder.DropCheckConstraint(
                name: "ck_authoring_rate_rows_merchandise_interval",
                schema: "shipping",
                table: "authoring_rate_rows");

            migrationBuilder.DropCheckConstraint(
                name: "ck_authoring_rate_rows_weight_interval",
                schema: "shipping",
                table: "authoring_rate_rows");

            migrationBuilder.AddCheckConstraint(
                name: "ck_authoring_region_groups_currency_canonical",
                schema: "shipping",
                table: "authoring_region_groups",
                sql: "currency_code IS NULL OR (char_length(currency_code) = 3 AND currency_code = upper(currency_code))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_authoring_rate_rows_amount_within_max",
                schema: "shipping",
                table: "authoring_rate_rows",
                sql: "freight_amount <= 1000000000 AND packaging_amount <= 1000000000 AND (max_merchandise_amount IS NULL OR max_merchandise_amount <= 1000000000)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_authoring_rate_rows_count_interval",
                schema: "shipping",
                table: "authoring_rate_rows",
                sql: "(min_item_count IS NULL AND max_item_count IS NULL) OR (min_item_count >= 1 AND min_item_count <= 1000000 AND (max_item_count IS NULL OR (max_item_count > min_item_count AND max_item_count <= 1000000)))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_authoring_rate_rows_merchandise_interval",
                schema: "shipping",
                table: "authoring_rate_rows",
                sql: "(min_merchandise_amount IS NULL AND max_merchandise_amount IS NULL) OR (min_merchandise_amount >= 0 AND (max_merchandise_amount IS NULL OR max_merchandise_amount > min_merchandise_amount))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_authoring_rate_rows_weight_interval",
                schema: "shipping",
                table: "authoring_rate_rows",
                sql: "(min_weight_grams IS NULL AND max_weight_grams IS NULL) OR (min_weight_grams >= 0 AND min_weight_grams <= 1000000000 AND (max_weight_grams IS NULL OR (max_weight_grams > min_weight_grams AND max_weight_grams <= 1000000000)))");
        }
    }
}
