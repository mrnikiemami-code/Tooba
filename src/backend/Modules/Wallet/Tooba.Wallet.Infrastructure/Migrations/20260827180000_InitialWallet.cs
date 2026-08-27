using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Wallet.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Wallet.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(WalletDbContext))]
    [Migration("20260827180000_InitialWallet")]
    public partial class InitialWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "wallet");

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "wallet",
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
                name: "wallet_accounts",
                schema: "wallet",
                columns: table => new
                {
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_accounts", x => x.account_id);
                });

            migrationBuilder.CreateTable(
                name: "wallet_ledger_entries",
                schema: "wallet",
                columns: table => new
                {
                    entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    direction = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    source_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wallet_ledger_entries", x => x.entry_id);
                });

            migrationBuilder.CreateTable(
                name: "gift_cards",
                schema: "wallet",
                columns: table => new
                {
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    initial_amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    remaining_amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    issued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    recipient_actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gift_cards", x => x.card_id);
                });

            migrationBuilder.CreateTable(
                name: "gift_card_redemptions",
                schema: "wallet",
                columns: table => new
                {
                    redemption_id = table.Column<Guid>(type: "uuid", nullable: false),
                    card_id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,0)", precision: 18, scale: 0, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_gift_card_redemptions", x => x.redemption_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_wallet_accounts_customer_actor_user_id",
                schema: "wallet",
                table: "wallet_accounts",
                column: "customer_actor_user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wallet_ledger_entries_account_id_created_at",
                schema: "wallet",
                table: "wallet_ledger_entries",
                columns: new[] { "account_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_wallet_ledger_entries_idempotency_key",
                schema: "wallet",
                table: "wallet_ledger_entries",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gift_cards_code_hash",
                schema: "wallet",
                table: "gift_cards",
                column: "code_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gift_cards_idempotency_key",
                schema: "wallet",
                table: "gift_cards",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_gift_cards_status_issued_at",
                schema: "wallet",
                table: "gift_cards",
                columns: new[] { "status", "issued_at" });

            migrationBuilder.CreateIndex(
                name: "ix_gift_card_redemptions_account_id_created_at",
                schema: "wallet",
                table: "gift_card_redemptions",
                columns: new[] { "account_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_gift_card_redemptions_card_id_created_at",
                schema: "wallet",
                table: "gift_card_redemptions",
                columns: new[] { "card_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_gift_card_redemptions_idempotency_key",
                schema: "wallet",
                table: "gift_card_redemptions",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_processed_at_next_attempt_at",
                schema: "wallet",
                table: "outbox_messages",
                columns: new[] { "processed_at", "next_attempt_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "gift_card_redemptions", schema: "wallet");
            migrationBuilder.DropTable(name: "gift_cards", schema: "wallet");
            migrationBuilder.DropTable(name: "wallet_ledger_entries", schema: "wallet");
            migrationBuilder.DropTable(name: "wallet_accounts", schema: "wallet");
            migrationBuilder.DropTable(name: "outbox_messages", schema: "wallet");
        }
    }
}
