using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptionLevelRatePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_rate_policies_service_scope",
                schema: "shipping",
                table: "rate_policies");

            migrationBuilder.AddColumn<string>(
                name: "option_code",
                schema: "shipping",
                table: "rate_policies",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ux_rate_policies_service_option_scope",
                schema: "shipping",
                table: "rate_policies",
                columns: new[] { "service_code", "option_code", "country_id", "administrative_area_id", "city_id", "service_area_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.AddCheckConstraint(
                name: "ck_rate_policies_option_code_canonical",
                schema: "shipping",
                table: "rate_policies",
                sql: "option_code IS NULL OR (char_length(option_code) > 0 AND option_code = btrim(option_code))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_rate_policies_service_option_scope",
                schema: "shipping",
                table: "rate_policies");

            migrationBuilder.DropCheckConstraint(
                name: "ck_rate_policies_option_code_canonical",
                schema: "shipping",
                table: "rate_policies");

            migrationBuilder.DropColumn(
                name: "option_code",
                schema: "shipping",
                table: "rate_policies");

            migrationBuilder.CreateIndex(
                name: "ux_rate_policies_service_scope",
                schema: "shipping",
                table: "rate_policies",
                columns: new[] { "service_code", "country_id", "administrative_area_id", "city_id", "service_area_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }
    }
}
