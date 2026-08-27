using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Returns.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Returns.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ReturnsDbContext))]
    [Migration("20260827200000_AddRefundDestination")]
    public partial class AddRefundDestination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "refund_destination",
                schema: "returns",
                table: "return_requests",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "OriginalPayment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "refund_destination",
                schema: "returns",
                table: "return_requests");
        }
    }
}
