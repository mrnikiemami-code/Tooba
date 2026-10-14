using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Geography.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Geography.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialGeography : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "geography");

            migrationBuilder.CreateTable(
                name: "countries",
                schema: "geography",
                columns: table => new
                {
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.country_id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "geography",
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
                name: "administrative_areas",
                schema: "geography",
                columns: table => new
                {
                    administrative_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_administrative_area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_administrative_areas", x => x.administrative_area_id);
                    table.UniqueConstraint("ak_administrative_areas_id_country", x => new { x.administrative_area_id, x.country_id });
                    table.ForeignKey(
                        name: "fk_administrative_areas_administrative_areas_parent_administra",
                        columns: x => new { x.parent_administrative_area_id, x.country_id },
                        principalSchema: "geography",
                        principalTable: "administrative_areas",
                        principalColumns: new[] { "administrative_area_id", "country_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_administrative_areas_countries_country_id",
                        column: x => x.country_id,
                        principalSchema: "geography",
                        principalTable: "countries",
                        principalColumn: "country_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "country_name_translations",
                schema: "geography",
                columns: table => new
                {
                    translation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_country_name_translations", x => x.translation_id);
                    table.ForeignKey(
                        name: "fk_country_name_translations_countries_country_id",
                        column: x => x.country_id,
                        principalSchema: "geography",
                        principalTable: "countries",
                        principalColumn: "country_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "administrative_area_name_translations",
                schema: "geography",
                columns: table => new
                {
                    translation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    administrative_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_administrative_area_name_translations", x => x.translation_id);
                    table.ForeignKey(
                        name: "fk_administrative_area_name_translations_administrative_areas_",
                        column: x => x.administrative_area_id,
                        principalSchema: "geography",
                        principalTable: "administrative_areas",
                        principalColumn: "administrative_area_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cities",
                schema: "geography",
                columns: table => new
                {
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    administrative_area_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cities", x => x.city_id);
                    table.ForeignKey(
                        name: "fk_cities_administrative_areas_administrative_area_id_country_",
                        columns: x => new { x.administrative_area_id, x.country_id },
                        principalSchema: "geography",
                        principalTable: "administrative_areas",
                        principalColumns: new[] { "administrative_area_id", "country_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cities_countries_country_id",
                        column: x => x.country_id,
                        principalSchema: "geography",
                        principalTable: "countries",
                        principalColumn: "country_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "city_name_translations",
                schema: "geography",
                columns: table => new
                {
                    translation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_city_name_translations", x => x.translation_id);
                    table.ForeignKey(
                        name: "fk_city_name_translations_cities_city_id",
                        column: x => x.city_id,
                        principalSchema: "geography",
                        principalTable: "cities",
                        principalColumn: "city_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_areas",
                schema: "geography",
                columns: table => new
                {
                    service_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_areas", x => x.service_area_id);
                    table.ForeignKey(
                        name: "fk_service_areas_cities_city_id",
                        column: x => x.city_id,
                        principalSchema: "geography",
                        principalTable: "cities",
                        principalColumn: "city_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "service_area_name_translations",
                schema: "geography",
                columns: table => new
                {
                    translation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_area_name_translations", x => x.translation_id);
                    table.ForeignKey(
                        name: "fk_service_area_name_translations_service_areas_service_area_id",
                        column: x => x.service_area_id,
                        principalSchema: "geography",
                        principalTable: "service_areas",
                        principalColumn: "service_area_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_administrative_area_name_translations_administrative_area_i",
                schema: "geography",
                table: "administrative_area_name_translations",
                columns: new[] { "administrative_area_id", "language_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_administrative_areas_country_id_code",
                schema: "geography",
                table: "administrative_areas",
                columns: new[] { "country_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_administrative_areas_country_id_is_active",
                schema: "geography",
                table: "administrative_areas",
                columns: new[] { "country_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_administrative_areas_parent_administrative_area_id_country_",
                schema: "geography",
                table: "administrative_areas",
                columns: new[] { "parent_administrative_area_id", "country_id" });

            migrationBuilder.CreateIndex(
                name: "ix_cities_administrative_area_id_country_id",
                schema: "geography",
                table: "cities",
                columns: new[] { "administrative_area_id", "country_id" });

            migrationBuilder.CreateIndex(
                name: "ix_cities_country_id_administrative_area_id_is_active",
                schema: "geography",
                table: "cities",
                columns: new[] { "country_id", "administrative_area_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_cities_country_id_code",
                schema: "geography",
                table: "cities",
                columns: new[] { "country_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_city_name_translations_city_id_language_id",
                schema: "geography",
                table: "city_name_translations",
                columns: new[] { "city_id", "language_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_countries_country_code",
                schema: "geography",
                table: "countries",
                column: "country_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_countries_is_active",
                schema: "geography",
                table: "countries",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_country_name_translations_country_id_language_id",
                schema: "geography",
                table: "country_name_translations",
                columns: new[] { "country_id", "language_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                schema: "geography",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL AND dead_lettered_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_service_area_name_translations_service_area_id_language_id",
                schema: "geography",
                table: "service_area_name_translations",
                columns: new[] { "service_area_id", "language_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_areas_city_id_code",
                schema: "geography",
                table: "service_areas",
                columns: new[] { "city_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_areas_city_id_is_active",
                schema: "geography",
                table: "service_areas",
                columns: new[] { "city_id", "is_active" });

            // Ancestry integrity: the model alone cannot express "no ancestor cycle" (a CHECK or FK
            // cannot hold a recursive property), so the invariant is carried by a module-owned
            // PostgreSQL trigger. It fires on INSERT as well as UPDATE - a pair of rows created by one
            // multi-row INSERT can reference each other, and the composite FK (an end-of-statement
            // check) accepts that pair - and it is an immediate trigger so no deferred/commit-time
            // window exists. See AdministrativeAreaAncestryGuard for the strategy, the concurrency
            // serialization rule and its transactional assumptions.
            migrationBuilder.Sql(AdministrativeAreaAncestryGuard.FunctionSql);

            migrationBuilder.Sql(AdministrativeAreaAncestryGuard.TriggerSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(AdministrativeAreaAncestryGuard.DropTriggerSql);

            migrationBuilder.Sql(AdministrativeAreaAncestryGuard.DropFunctionSql);

            migrationBuilder.DropTable(
                name: "administrative_area_name_translations",
                schema: "geography");

            migrationBuilder.DropTable(
                name: "city_name_translations",
                schema: "geography");

            migrationBuilder.DropTable(
                name: "country_name_translations",
                schema: "geography");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "geography");

            migrationBuilder.DropTable(
                name: "service_area_name_translations",
                schema: "geography");

            migrationBuilder.DropTable(
                name: "service_areas",
                schema: "geography");

            migrationBuilder.DropTable(
                name: "cities",
                schema: "geography");

            migrationBuilder.DropTable(
                name: "administrative_areas",
                schema: "geography");

            migrationBuilder.DropTable(
                name: "countries",
                schema: "geography");
        }
    }
}
