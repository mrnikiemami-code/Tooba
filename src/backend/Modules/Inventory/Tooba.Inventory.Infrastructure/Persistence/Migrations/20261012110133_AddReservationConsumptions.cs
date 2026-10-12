using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Inventory.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(InventoryDbContext))]
    [Migration("20261012110133_AddReservationConsumptions")]
    public partial class AddReservationConsumptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ژورنال مصرف رزرو: PK = reservation_id، پس هر رزرو حداکثر یک مصرف پایدار دارد.
            // هیچ ردیفی backfill نمی‌شود؛ رزروهای Consumed قدیمی بدون ژورنال می‌مانند تا
            // provenance جعلی ساخته نشود و replay اثبات‌نشده پذیرفته نشود.
            migrationBuilder.CreateTable(
                name: "reservation_consumptions",
                schema: "inventory",
                columns: table => new
                {
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    consumer_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reservation_consumptions", x => x.reservation_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reservation_consumptions",
                schema: "inventory");
        }
    }
}
