using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrderDbContext))]
    [Migration("20260907010000_CheckoutOperationalNotes")]
    public partial class CheckoutOperationalNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "checkout_operational_notes",
                schema: "order",
                columns: table => new
                {
                    note_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checkout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checkout_operational_notes", x => x.note_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_checkout_operational_notes_checkout_id_created_at",
                schema: "order",
                table: "checkout_operational_notes",
                columns: new[] { "checkout_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checkout_operational_notes",
                schema: "order");
        }
    }
}
