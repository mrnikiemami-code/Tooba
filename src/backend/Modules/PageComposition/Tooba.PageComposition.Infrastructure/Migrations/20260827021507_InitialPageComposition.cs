using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.PageComposition.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPageComposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "page_composition");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "page_composition",
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
                name: "page_definitions",
                schema: "page_composition",
                columns: table => new
                {
                    page_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    version_token = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_page_definitions", x => x.page_definition_id);
                });

            migrationBuilder.CreateTable(
                name: "page_sections",
                schema: "page_composition",
                columns: table => new
                {
                    page_section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    variant = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    configuration_json = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_page_sections", x => x.page_section_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                schema: "page_composition",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL AND dead_lettered_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_page_definitions_tenant_id_page_key_locale",
                schema: "page_composition",
                table: "page_definitions",
                columns: new[] { "tenant_id", "page_key", "locale" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_page_sections_page_definition_id_display_order",
                schema: "page_composition",
                table: "page_sections",
                columns: new[] { "page_definition_id", "display_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "page_composition");

            migrationBuilder.DropTable(
                name: "page_definitions",
                schema: "page_composition");

            migrationBuilder.DropTable(
                name: "page_sections",
                schema: "page_composition");
        }
    }
}
