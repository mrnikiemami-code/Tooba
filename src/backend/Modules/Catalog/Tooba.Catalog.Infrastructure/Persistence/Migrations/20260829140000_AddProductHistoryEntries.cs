using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductHistoryEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "product_history_entries",
                schema: "catalog",
                columns: table => new
                {
                    history_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    section = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    summary_fa = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    before_summary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    after_summary = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_product_history_entries", x => x.history_id);
                    table.ForeignKey(
                        name: "fk_product_history_entries_products_product_id",
                        column: x => x.product_id,
                        principalSchema: "catalog",
                        principalTable: "products",
                        principalColumn: "product_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_product_history_entries_product_id_occurred_at",
                schema: "catalog",
                table: "product_history_entries",
                columns: new[] { "product_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_product_history_entries_product_id_section_occurred_at",
                schema: "catalog",
                table: "product_history_entries",
                columns: new[] { "product_id", "section", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "product_history_entries",
                schema: "catalog");
        }
    }
}
