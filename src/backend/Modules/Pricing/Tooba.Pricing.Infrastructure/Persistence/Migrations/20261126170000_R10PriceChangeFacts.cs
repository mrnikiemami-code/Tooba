using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Pricing.Infrastructure.Persistence.Migrations
{
    /// <summary>R10: durable Pricing-owned price change facts committed with the price mutation.</summary>
    [DbContext(typeof(PricingDbContext))]
    [Migration("20261126170000_R10PriceChangeFacts")]
    public partial class R10PriceChangeFacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "offer_price_changes",
                schema: "pricing",
                columns: table => new
                {
                    fact_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    price_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    old_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: true),
                    new_amount = table.Column<decimal>(type: "numeric(19,4)", precision: 19, scale: 4, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    revision = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offer_price_changes", x => x.fact_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_offer_price_changes_offer_id_occurred_at",
                schema: "pricing",
                table: "offer_price_changes",
                columns: new[] { "offer_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_offer_price_changes_operation_id",
                schema: "pricing",
                table: "offer_price_changes",
                column: "operation_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "offer_price_changes", schema: "pricing");
        }
    }
}
