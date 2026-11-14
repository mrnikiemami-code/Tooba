using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Returns.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-265-R2: additive schema for the durable, serialized approve/reject decision of one return case.
    /// <para>
    /// Before this table the two operator paths read the case status independently, so two requests could both see
    /// <c>Requested</c> and then approve (refund + claim release) while the other rejected (claim release). The primary
    /// key on <c>return_request_id</c> makes the FIRST committed decision durable and unique; the caller reads it under
    /// the case row lock before any money, stock or claim effect. No existing table or row is altered.
    /// </para>
    /// </summary>
    [DbContext(typeof(ReturnsDbContext))]
    [Migration("20261114170000_AddReturnRequestDecisions")]
    public partial class AddReturnRequestDecisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "return_request_decisions",
                schema: "returns",
                columns: table => new
                {
                    return_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    decided_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_return_request_decisions", x => x.return_request_id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "return_request_decisions",
                schema: "returns");
        }
    }
}
