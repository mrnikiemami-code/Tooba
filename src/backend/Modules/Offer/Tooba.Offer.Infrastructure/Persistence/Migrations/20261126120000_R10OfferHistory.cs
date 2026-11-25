using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Offer.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Offer.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// R10-06: append-only Offer-owned history projection. Additive only; no existing table is altered and
    /// no historical row is fabricated or backfilled.
    /// </summary>
    [DbContext(typeof(OfferDbContext))]
    [Migration("20261126120000_R10OfferHistory")]
    public partial class R10OfferHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "offer_history",
                schema: "offer",
                columns: table => new
                {
                    offer_history_entry_id = table.Column<Guid>(type: "uuid", nullable: false),
                    offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    change_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    summary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    old_value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    new_value = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_offer_history", x => x.offer_history_entry_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_offer_history_offer_id_occurred_at",
                schema: "offer",
                table: "offer_history",
                columns: new[] { "offer_id", "occurred_at" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "offer_history", schema: "offer");
        }
    }
}
