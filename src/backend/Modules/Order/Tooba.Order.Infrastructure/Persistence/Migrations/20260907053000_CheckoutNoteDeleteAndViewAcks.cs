using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(OrderDbContext))]
    [Migration("20260907053000_CheckoutNoteDeleteAndViewAcks")]
    public partial class CheckoutNoteDeleteAndViewAcks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deleted_at",
                schema: "order",
                table: "checkout_operational_notes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                schema: "order",
                table: "checkout_operational_notes",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "checkout_admin_view_acks",
                schema: "order",
                columns: table => new
                {
                    ack_id = table.Column<Guid>(type: "uuid", nullable: false),
                    checkout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    viewer_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    viewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_checkout_admin_view_acks", x => x.ack_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_checkout_admin_view_acks_checkout_id_viewed_at",
                schema: "order",
                table: "checkout_admin_view_acks",
                columns: new[] { "checkout_id", "viewed_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "checkout_admin_view_acks",
                schema: "order");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                schema: "order",
                table: "checkout_operational_notes");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                schema: "order",
                table: "checkout_operational_notes");
        }
    }
}
