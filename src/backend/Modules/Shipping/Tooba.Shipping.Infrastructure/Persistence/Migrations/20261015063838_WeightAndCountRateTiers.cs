using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WeightAndCountRateTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_rate_policies_service_option_scope",
                schema: "shipping",
                table: "rate_policies");

            migrationBuilder.AddColumn<int>(
                name: "max_item_count",
                schema: "shipping",
                table: "rate_policies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "max_weight_grams",
                schema: "shipping",
                table: "rate_policies",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "min_item_count",
                schema: "shipping",
                table: "rate_policies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "min_weight_grams",
                schema: "shipping",
                table: "rate_policies",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_rate_policies_service_option_scope",
                schema: "shipping",
                table: "rate_policies",
                columns: new[] { "service_code", "option_code", "country_id", "administrative_area_id", "city_id", "service_area_id", "min_weight_grams", "max_weight_grams", "min_item_count", "max_item_count" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.AddCheckConstraint(
                name: "ck_rate_policies_count_tier_coherent",
                schema: "shipping",
                table: "rate_policies",
                sql: "min_item_count IS NULL OR (min_item_count >= 0 AND min_item_count <= 1000000 AND (max_item_count IS NULL OR (max_item_count > min_item_count AND max_item_count <= 1000000)))");

            migrationBuilder.AddCheckConstraint(
                name: "ck_rate_policies_tier_bounds_paired",
                schema: "shipping",
                table: "rate_policies",
                sql: "(max_weight_grams IS NULL OR min_weight_grams IS NOT NULL) AND (max_item_count IS NULL OR min_item_count IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_rate_policies_weight_tier_coherent",
                schema: "shipping",
                table: "rate_policies",
                sql: "min_weight_grams IS NULL OR (min_weight_grams >= 0 AND min_weight_grams <= 1000000000 AND (max_weight_grams IS NULL OR (max_weight_grams > min_weight_grams AND max_weight_grams <= 1000000000)))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_rate_policies_service_option_scope",
                schema: "shipping",
                table: "rate_policies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_rate_policies_count_tier_coherent",
                schema: "shipping",
                table: "rate_policies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_rate_policies_tier_bounds_paired",
                schema: "shipping",
                table: "rate_policies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_rate_policies_weight_tier_coherent",
                schema: "shipping",
                table: "rate_policies");

            migrationBuilder.DropColumn(
                name: "max_item_count",
                schema: "shipping",
                table: "rate_policies");

            migrationBuilder.DropColumn(
                name: "max_weight_grams",
                schema: "shipping",
                table: "rate_policies");

            migrationBuilder.DropColumn(
                name: "min_item_count",
                schema: "shipping",
                table: "rate_policies");

            migrationBuilder.DropColumn(
                name: "min_weight_grams",
                schema: "shipping",
                table: "rate_policies");

            migrationBuilder.CreateIndex(
                name: "ux_rate_policies_service_option_scope",
                schema: "shipping",
                table: "rate_policies",
                columns: new[] { "service_code", "option_code", "country_id", "administrative_area_id", "city_id", "service_area_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }
    }
}
