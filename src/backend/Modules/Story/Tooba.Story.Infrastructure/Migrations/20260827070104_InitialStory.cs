using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Story.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialStory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "story");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "story",
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
                name: "stories",
                schema: "story",
                columns: table => new
                {
                    story_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    market = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    cover_media_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    cover_media_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    cta_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cta_target = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    version_token = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stories", x => x.story_id);
                });

            migrationBuilder.CreateTable(
                name: "story_items",
                schema: "story",
                columns: table => new
                {
                    story_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    story_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    media_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    media_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    media_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    caption = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: true),
                    cta_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    cta_target = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_story_items", x => x.story_item_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                schema: "story",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL AND dead_lettered_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_stories_tenant_id_display_order",
                schema: "story",
                table: "stories",
                columns: new[] { "tenant_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_stories_tenant_id_status",
                schema: "story",
                table: "stories",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_story_items_story_id_display_order",
                schema: "story",
                table: "story_items",
                columns: new[] { "story_id", "display_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "story");

            migrationBuilder.DropTable(
                name: "stories",
                schema: "story");

            migrationBuilder.DropTable(
                name: "story_items",
                schema: "story");
        }
    }
}
