using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Notification.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialNotification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notification");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "notification",
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
                name: "user_notifications",
                schema: "notification",
                columns: table => new
                {
                    notification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_kind = table.Column<int>(type: "integer", nullable: false),
                    recipient_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    target_route = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    source_event_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    source_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_notifications", x => x.notification_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                schema: "notification",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL AND dead_lettered_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_user_notifications_recipient_kind_recipient_actor_user_id_c",
                schema: "notification",
                table: "user_notifications",
                columns: new[] { "recipient_kind", "recipient_actor_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_user_notifications_recipient_kind_recipient_party_id_create",
                schema: "notification",
                table: "user_notifications",
                columns: new[] { "recipient_kind", "recipient_party_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_user_notifications_recipient_kind_recipient_party_id_is_rea",
                schema: "notification",
                table: "user_notifications",
                columns: new[] { "recipient_kind", "recipient_party_id", "is_read", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_user_notifications_recipient_kind_recipient_party_id_source",
                schema: "notification",
                table: "user_notifications",
                columns: new[] { "recipient_kind", "recipient_party_id", "source_event_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "user_notifications",
                schema: "notification");
        }
    }
}
