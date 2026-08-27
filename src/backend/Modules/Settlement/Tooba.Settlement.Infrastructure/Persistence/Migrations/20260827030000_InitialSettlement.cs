using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Settlement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "settlement");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "settlement",
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
                    locked_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("pk_outbox_messages", x => x.id));

            migrationBuilder.CreateTable(
                name: "payment_inbox",
                schema: "settlement",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_payment_inbox", x => x.event_id));

            migrationBuilder.CreateTable(
                name: "refund_inbox",
                schema: "settlement",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    return_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_refund_inbox", x => x.event_id));

            migrationBuilder.CreateTable(
                name: "commission_policies",
                schema: "settlement",
                columns: table => new
                {
                    policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    rate = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    effective_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_commission_policies", x => x.policy_id));

            migrationBuilder.CreateIndex(
                name: "ix_commission_policies_is_default",
                schema: "settlement",
                table: "commission_policies",
                column: "is_default");

            migrationBuilder.CreateTable(
                name: "settlement_accounts",
                schema: "settlement",
                columns: table => new
                {
                    settlement_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_settlement_accounts", x => x.settlement_account_id));

            migrationBuilder.CreateIndex(
                name: "ix_settlement_accounts_seller_party_id",
                schema: "settlement",
                table: "settlement_accounts",
                column: "seller_party_id",
                unique: true);

            migrationBuilder.CreateTable(
                name: "settlement_entries",
                schema: "settlement",
                columns: table => new
                {
                    entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    settlement_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    gross_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    commission_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    net_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    commission_policy_id = table.Column<Guid>(type: "uuid", nullable: false),
                    commission_policy_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    commission_rate = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    source_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    posted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_settlement_entries", x => x.entry_id));

            migrationBuilder.CreateIndex(
                name: "ix_settlement_entries_idempotency_key",
                schema: "settlement",
                table: "settlement_entries",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_settlement_entries_seller_order_id",
                schema: "settlement",
                table: "settlement_entries",
                column: "seller_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_settlement_entries_seller_party_id",
                schema: "settlement",
                table: "settlement_entries",
                column: "seller_party_id");

            migrationBuilder.CreateIndex(
                name: "ix_settlement_entries_settlement_account_id",
                schema: "settlement",
                table: "settlement_entries",
                column: "settlement_account_id");

            migrationBuilder.CreateTable(
                name: "settlement_statements",
                schema: "settlement",
                columns: table => new
                {
                    statement_id = table.Column<Guid>(type: "uuid", nullable: false),
                    settlement_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    period_start = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    period_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    opening_balance = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    closing_balance = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_settlement_statements", x => x.statement_id));

            migrationBuilder.CreateIndex(
                name: "ix_settlement_statements_settlement_account_id",
                schema: "settlement",
                table: "settlement_statements",
                column: "settlement_account_id");

            migrationBuilder.CreateTable(
                name: "seller_payout_profiles",
                schema: "settlement",
                columns: table => new
                {
                    seller_payout_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    iban = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    account_holder_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_seller_payout_profiles", x => x.seller_payout_profile_id));

            migrationBuilder.CreateIndex(
                name: "ix_seller_payout_profiles_seller_party_id",
                schema: "settlement",
                table: "seller_payout_profiles",
                column: "seller_party_id",
                unique: true);

            migrationBuilder.CreateTable(
                name: "payout_requests",
                schema: "settlement",
                columns: table => new
                {
                    payout_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    settlement_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table => table.PrimaryKey("pk_payout_requests", x => x.payout_request_id));

            migrationBuilder.CreateIndex(
                name: "ix_payout_requests_idempotency_key",
                schema: "settlement",
                table: "payout_requests",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payout_requests_seller_party_id",
                schema: "settlement",
                table: "payout_requests",
                column: "seller_party_id");

            migrationBuilder.CreateIndex(
                name: "ix_payout_requests_settlement_account_id",
                schema: "settlement",
                table: "payout_requests",
                column: "settlement_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_payout_requests_status",
                schema: "settlement",
                table: "payout_requests",
                column: "status");

            migrationBuilder.CreateTable(
                name: "payout_attempts",
                schema: "settlement",
                columns: table => new
                {
                    payout_attempt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payout_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    provider_reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    failure_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                },
                constraints: table => table.PrimaryKey("pk_payout_attempts", x => x.payout_attempt_id));

            migrationBuilder.CreateIndex(
                name: "ix_payout_attempts_idempotency_key",
                schema: "settlement",
                table: "payout_attempts",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payout_attempts_payout_request_id",
                schema: "settlement",
                table: "payout_attempts",
                column: "payout_request_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "outbox_messages", schema: "settlement");
            migrationBuilder.DropTable(name: "payment_inbox", schema: "settlement");
            migrationBuilder.DropTable(name: "refund_inbox", schema: "settlement");
            migrationBuilder.DropTable(name: "settlement_entries", schema: "settlement");
            migrationBuilder.DropTable(name: "settlement_statements", schema: "settlement");
            migrationBuilder.DropTable(name: "payout_attempts", schema: "settlement");
            migrationBuilder.DropTable(name: "payout_requests", schema: "settlement");
            migrationBuilder.DropTable(name: "seller_payout_profiles", schema: "settlement");
            migrationBuilder.DropTable(name: "settlement_accounts", schema: "settlement");
            migrationBuilder.DropTable(name: "commission_policies", schema: "settlement");
        }
    }
}
