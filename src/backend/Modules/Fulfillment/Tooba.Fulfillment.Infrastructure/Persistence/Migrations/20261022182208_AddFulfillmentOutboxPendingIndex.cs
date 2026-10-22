using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFulfillmentOutboxPendingIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // شکاف تاریخی: نگاشت مشترک Outbox در همهٔ ماژول‌ها این ایندکس را اعلام می‌کند، اما مهاجرت اولیهٔ
            // Fulfillment فقط جدول و کلید اصلی را ساخت. دیسپچر با همین predicate روی occurred_at مرتب می‌کند،
            // پس بدون این ایندکس هر poll روی کل تاریخچهٔ پردازش‌شده اسکن/سورت می‌کند.
            migrationBuilder.CreateIndex(
                name: "ix_outbox_messages_pending",
                schema: "fulfillment",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at IS NULL AND dead_lettered_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outbox_messages_pending",
                schema: "fulfillment",
                table: "outbox_messages");
        }
    }
}
