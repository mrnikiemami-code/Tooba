using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialShipping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "shipping");

            migrationBuilder.CreateTable(
                name: "coverage_grants",
                schema: "shipping",
                columns: table => new
                {
                    coverage_grant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    administrative_area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    city_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coverage_grants", x => x.coverage_grant_id);
                    table.CheckConstraint("ck_coverage_grants_scope_coherent", "service_area_id IS NULL OR city_id IS NOT NULL");
                    table.CheckConstraint("ck_coverage_grants_service_code_canonical", "char_length(service_code) > 0 AND service_code = btrim(service_code)");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "shipping",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    event_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    deployment_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    edition = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dead_lettered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "rate_policies",
                schema: "shipping",
                columns: table => new
                {
                    rate_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    administrative_area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    city_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    charge_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    preparation_days = table.Column<int>(type: "integer", nullable: false),
                    lead_days = table.Column<int>(type: "integer", nullable: false),
                    free_above_threshold = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rate_policies", x => x.rate_policy_id);
                    table.CheckConstraint("ck_rate_policies_charge_amount_non_negative", "charge_amount >= 0");
                    table.CheckConstraint("ck_rate_policies_charge_amount_within_max", "charge_amount <= 1000000000");
                    table.CheckConstraint("ck_rate_policies_currency_code_canonical", "char_length(currency_code) = 3 AND currency_code = upper(currency_code)");
                    table.CheckConstraint("ck_rate_policies_free_above_threshold_non_negative", "free_above_threshold IS NULL OR free_above_threshold >= 0");
                    table.CheckConstraint("ck_rate_policies_lead_days_range", "lead_days >= 0 AND lead_days <= 365");
                    table.CheckConstraint("ck_rate_policies_preparation_days_range", "preparation_days >= 0 AND preparation_days <= 365");
                    table.CheckConstraint("ck_rate_policies_scope_coherent", "service_area_id IS NULL OR city_id IS NOT NULL");
                    table.CheckConstraint("ck_rate_policies_service_code_canonical", "char_length(service_code) > 0 AND service_code = btrim(service_code)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_coverage_grants_service_country_active",
                schema: "shipping",
                table: "coverage_grants",
                columns: new[] { "service_code", "country_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ux_coverage_grants_service_scope",
                schema: "shipping",
                table: "coverage_grants",
                columns: new[] { "service_code", "country_id", "administrative_area_id", "city_id", "service_area_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                schema: "shipping",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL AND dead_lettered_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_rate_policies_service_active",
                schema: "shipping",
                table: "rate_policies",
                columns: new[] { "service_code", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ux_rate_policies_service_scope",
                schema: "shipping",
                table: "rate_policies",
                columns: new[] { "service_code", "country_id", "administrative_area_id", "city_id", "service_area_id" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "coverage_grants",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "shipping");

            migrationBuilder.DropTable(
                name: "rate_policies",
                schema: "shipping");
        }
    }
}
