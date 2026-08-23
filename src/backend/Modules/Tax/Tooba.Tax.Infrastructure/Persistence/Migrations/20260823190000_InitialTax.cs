using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Tax.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialTax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "tax");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "tax",
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
                name: "categories",
                schema: "tax",
                columns: table => new
                {
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.category_id);
                });

            migrationBuilder.CreateTable(
                name: "offer_classifications",
                schema: "tax",
                columns: table => new
                {
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offer_classifications", x => x.offer_id);
                });

            migrationBuilder.CreateTable(
                name: "rules",
                schema: "tax",
                columns: table => new
                {
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    jurisdiction = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    market = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(19,8)", precision: 19, scale: 8, nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    effective_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    specificity = table.Column<int>(type: "integer", nullable: false),
                    override_policy = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rules", x => x.rule_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                schema: "tax",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL AND dead_lettered_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_categories_code",
                schema: "tax",
                table: "categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rules_jurisdiction_market_category_id_effective_from",
                schema: "tax",
                table: "rules",
                columns: new[] { "jurisdiction", "market", "category_id", "effective_from" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "outbox_messages", schema: "tax");
            migrationBuilder.DropTable(name: "offer_classifications", schema: "tax");
            migrationBuilder.DropTable(name: "rules", schema: "tax");
            migrationBuilder.DropTable(name: "categories", schema: "tax");
        }
    }
}
