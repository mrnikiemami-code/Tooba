using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Payment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PaymentWebhookInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "webhook_inbox",
                schema: "payment",
                columns: table => new
                {
                    inbox_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provider_event_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_inbox", x => x.inbox_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_inbox_provider_code_provider_event_id",
                schema: "payment",
                table: "webhook_inbox",
                columns: new[] { "provider_code", "provider_event_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "webhook_inbox",
                schema: "payment");
        }
    }
}
