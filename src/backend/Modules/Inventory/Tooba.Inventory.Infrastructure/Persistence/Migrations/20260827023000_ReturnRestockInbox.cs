using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Inventory.Infrastructure.Persistence.Migrations;

/// <inheritdoc />
public partial class ReturnRestockInbox : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "return_restock_inbox",
            schema: "inventory",
            columns: table => new
            {
                idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                reservation_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<int>(type: "integer", nullable: false),
                processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_return_restock_inbox", x => x.idempotency_key);
            });

        migrationBuilder.CreateIndex(
            name: "IX_return_restock_inbox_reservation_id",
            schema: "inventory",
            table: "return_restock_inbox",
            column: "reservation_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "return_restock_inbox",
            schema: "inventory");
    }
}
