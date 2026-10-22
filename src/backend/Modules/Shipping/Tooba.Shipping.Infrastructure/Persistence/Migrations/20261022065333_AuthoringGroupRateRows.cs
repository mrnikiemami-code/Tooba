using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuthoringGroupRateRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "currency_code",
                schema: "shipping",
                table: "authoring_region_groups",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "authoring_rate_rows",
                schema: "shipping",
                columns: table => new
                {
                    rate_row_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    min_weight_grams = table.Column<long>(type: "bigint", nullable: true),
                    max_weight_grams = table.Column<long>(type: "bigint", nullable: true),
                    min_item_count = table.Column<int>(type: "integer", nullable: true),
                    max_item_count = table.Column<int>(type: "integer", nullable: true),
                    min_merchandise_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    max_merchandise_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    freight_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    packaging_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    min_delivery_days = table.Column<int>(type: "integer", nullable: false),
                    max_delivery_days = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_authoring_rate_rows", x => x.rate_row_id);
                    table.CheckConstraint("ck_authoring_rate_rows_amount_within_max", "freight_amount <= 1000000000 AND packaging_amount <= 1000000000 AND (max_merchandise_amount IS NULL OR max_merchandise_amount <= 1000000000)");
                    table.CheckConstraint("ck_authoring_rate_rows_count_interval", "(min_item_count IS NULL AND max_item_count IS NULL) OR (min_item_count >= 1 AND min_item_count <= 1000000 AND (max_item_count IS NULL OR (max_item_count > min_item_count AND max_item_count <= 1000000)))");
                    table.CheckConstraint("ck_authoring_rate_rows_duration_range", "min_delivery_days >= 0 AND max_delivery_days >= min_delivery_days AND max_delivery_days <= 365");
                    table.CheckConstraint("ck_authoring_rate_rows_freight_non_negative", "freight_amount >= 0");
                    table.CheckConstraint("ck_authoring_rate_rows_merchandise_interval", "(min_merchandise_amount IS NULL AND max_merchandise_amount IS NULL) OR (min_merchandise_amount >= 0 AND (max_merchandise_amount IS NULL OR max_merchandise_amount > min_merchandise_amount))");
                    table.CheckConstraint("ck_authoring_rate_rows_packaging_non_negative", "packaging_amount >= 0");
                    table.CheckConstraint("ck_authoring_rate_rows_weight_interval", "(min_weight_grams IS NULL AND max_weight_grams IS NULL) OR (min_weight_grams >= 0 AND min_weight_grams <= 1000000000 AND (max_weight_grams IS NULL OR (max_weight_grams > min_weight_grams AND max_weight_grams <= 1000000000)))");
                    table.ForeignKey(
                        name: "fk_authoring_rate_rows_authoring_region_groups_group_id",
                        column: x => x.group_id,
                        principalSchema: "shipping",
                        principalTable: "authoring_region_groups",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_authoring_region_groups_currency_canonical",
                schema: "shipping",
                table: "authoring_region_groups",
                sql: "currency_code IS NULL OR (char_length(currency_code) = 3 AND currency_code = upper(currency_code))");

            migrationBuilder.CreateIndex(
                name: "ix_authoring_rate_rows_group",
                schema: "shipping",
                table: "authoring_rate_rows",
                column: "group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "authoring_rate_rows",
                schema: "shipping");

            migrationBuilder.DropCheckConstraint(
                name: "ck_authoring_region_groups_currency_canonical",
                schema: "shipping",
                table: "authoring_region_groups");

            migrationBuilder.DropColumn(
                name: "currency_code",
                schema: "shipping",
                table: "authoring_region_groups");
        }
    }
}
