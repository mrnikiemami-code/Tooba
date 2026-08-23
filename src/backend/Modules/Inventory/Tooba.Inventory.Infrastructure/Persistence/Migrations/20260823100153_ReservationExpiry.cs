using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Inventory.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReservationExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "expires_at",
                schema: "inventory",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "expires_at",
                schema: "inventory",
                table: "reservations");
        }
    }
}
