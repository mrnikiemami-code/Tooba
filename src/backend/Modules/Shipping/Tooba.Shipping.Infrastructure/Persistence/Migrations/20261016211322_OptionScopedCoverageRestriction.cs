using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OptionScopedCoverageRestriction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_coverage_grants_service_scope",
                schema: "shipping",
                table: "coverage_grants");

            migrationBuilder.AddColumn<string>(
                name: "option_code",
                schema: "shipping",
                table: "coverage_grants",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "method_coverages",
                schema: "shipping",
                columns: table => new
                {
                    method_coverage_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    option_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_method_coverages", x => x.method_coverage_id);
                    table.CheckConstraint("ck_method_coverages_mode_known", "mode IN (0, 1)");
                    table.CheckConstraint("ck_method_coverages_option_code_canonical", "option_code IS NULL OR (char_length(option_code) > 0 AND option_code = btrim(option_code))");
                    table.CheckConstraint("ck_method_coverages_service_code_canonical", "char_length(service_code) > 0 AND service_code = btrim(service_code)");
                });

            migrationBuilder.CreateIndex(
                name: "ux_coverage_grants_service_option_scope",
                schema: "shipping",
                table: "coverage_grants",
                columns: new[] { "service_code", "option_code", "country_id", "administrative_area_id", "city_id", "service_area_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.AddCheckConstraint(
                name: "ck_coverage_grants_option_code_canonical",
                schema: "shipping",
                table: "coverage_grants",
                sql: "option_code IS NULL OR (char_length(option_code) > 0 AND option_code = btrim(option_code))");

            migrationBuilder.CreateIndex(
                name: "ix_method_coverages_service",
                schema: "shipping",
                table: "method_coverages",
                column: "service_code");

            migrationBuilder.CreateIndex(
                name: "ux_method_coverages_service_option",
                schema: "shipping",
                table: "method_coverages",
                columns: new[] { "service_code", "option_code" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "method_coverages",
                schema: "shipping");

            migrationBuilder.DropIndex(
                name: "ux_coverage_grants_service_option_scope",
                schema: "shipping",
                table: "coverage_grants");

            migrationBuilder.DropCheckConstraint(
                name: "ck_coverage_grants_option_code_canonical",
                schema: "shipping",
                table: "coverage_grants");

            migrationBuilder.DropColumn(
                name: "option_code",
                schema: "shipping",
                table: "coverage_grants");

            migrationBuilder.CreateIndex(
                name: "ux_coverage_grants_service_scope",
                schema: "shipping",
                table: "coverage_grants",
                columns: new[] { "service_code", "country_id", "administrative_area_id", "city_id", "service_area_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }
    }
}
