using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.AccessControl.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAccessControl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "access_control");

            migrationBuilder.CreateTable(
                name: "access_audit_events",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    target_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    seller_scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    before_summary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    after_summary = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    trace_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_audit_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "access_control",
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
                name: "platform_seller_ceilings",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_platform_seller_ceilings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    permission_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    scope_kind = table.Column<int>(type: "integer", nullable: false),
                    scope_resource_id = table.Column<Guid>(type: "uuid", nullable: true),
                    enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role_permissions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    owner_scope_kind = table.Column<int>(type: "integer", nullable: false),
                    owner_scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_mutable = table.Column<bool>(type: "boolean", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_role_assignments",
                schema: "access_control",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_scope_kind = table.Column<int>(type: "integer", nullable: false),
                    owner_scope_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_role_assignments", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_audit_events_at",
                schema: "access_control",
                table: "access_audit_events",
                column: "at");

            migrationBuilder.CreateIndex(
                name: "ix_access_audit_events_seller_scope_id_at",
                schema: "access_control",
                table: "access_audit_events",
                columns: new[] { "seller_scope_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                schema: "access_control",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL AND dead_lettered_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_platform_seller_ceilings_seller_party_id",
                schema: "access_control",
                table: "platform_seller_ceilings",
                column: "seller_party_id");

            migrationBuilder.CreateIndex(
                name: "ix_platform_seller_ceilings_seller_party_id_permission_id",
                schema: "access_control",
                table: "platform_seller_ceilings",
                columns: new[] { "seller_party_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_role_id",
                schema: "access_control",
                table: "role_permissions",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_role_permissions_role_id_permission_id_scope_kind_scope_res",
                schema: "access_control",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id", "scope_kind", "scope_resource_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_owner_scope_kind_owner_scope_id_code",
                schema: "access_control",
                table: "roles",
                columns: new[] { "owner_scope_kind", "owner_scope_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roles_owner_scope_kind_owner_scope_id_is_archived",
                schema: "access_control",
                table: "roles",
                columns: new[] { "owner_scope_kind", "owner_scope_id", "is_archived" });

            migrationBuilder.CreateIndex(
                name: "ix_user_role_assignments_owner_scope_kind_owner_scope_id_user_",
                schema: "access_control",
                table: "user_role_assignments",
                columns: new[] { "owner_scope_kind", "owner_scope_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_user_role_assignments_role_id",
                schema: "access_control",
                table: "user_role_assignments",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_assignments_user_id_role_id_owner_scope_kind_owne",
                schema: "access_control",
                table: "user_role_assignments",
                columns: new[] { "user_id", "role_id", "owner_scope_kind", "owner_scope_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_audit_events",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "platform_seller_ceilings",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "role_permissions",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "access_control");

            migrationBuilder.DropTable(
                name: "user_role_assignments",
                schema: "access_control");
        }
    }
}
