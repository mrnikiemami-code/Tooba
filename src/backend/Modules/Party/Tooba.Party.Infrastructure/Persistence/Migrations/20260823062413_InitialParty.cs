using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Party.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialParty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "party");

            migrationBuilder.CreateTable(
                name: "memberships",
                schema: "party",
                columns: table => new
                {
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    relation_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_memberships", x => x.membership_id);
                });

            migrationBuilder.CreateTable(
                name: "organization_relationships",
                schema: "party",
                columns: table => new
                {
                    relationship_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relation_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_relationships", x => x.relationship_id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "party",
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
                name: "parties",
                schema: "party",
                columns: table => new
                {
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parties", x => x.party_id);
                });

            migrationBuilder.CreateTable(
                name: "user_party_links",
                schema: "party",
                columns: table => new
                {
                    link_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_party_links", x => x.link_id);
                });

            migrationBuilder.CreateTable(
                name: "party_capabilities",
                schema: "party",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    capability_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_party_capabilities", x => x.id);
                    table.ForeignKey(
                        name: "fk_party_capabilities_parties_party_id",
                        column: x => x.party_id,
                        principalSchema: "party",
                        principalTable: "parties",
                        principalColumn: "party_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_memberships_user_id_party_id_relation_code",
                schema: "party",
                table: "memberships",
                columns: new[] { "user_id", "party_id", "relation_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_organization_relationships_from_party_id_to_party_id_relati",
                schema: "party",
                table: "organization_relationships",
                columns: new[] { "from_party_id", "to_party_id", "relation_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                schema: "party",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL AND dead_lettered_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_party_capabilities_party_id_capability_code",
                schema: "party",
                table: "party_capabilities",
                columns: new[] { "party_id", "capability_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_party_links_user_id_party_id",
                schema: "party",
                table: "user_party_links",
                columns: new[] { "user_id", "party_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "memberships",
                schema: "party");

            migrationBuilder.DropTable(
                name: "organization_relationships",
                schema: "party");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "party");

            migrationBuilder.DropTable(
                name: "party_capabilities",
                schema: "party");

            migrationBuilder.DropTable(
                name: "user_party_links",
                schema: "party");

            migrationBuilder.DropTable(
                name: "parties",
                schema: "party");
        }
    }
}
