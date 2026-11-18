using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Media.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Tb281SellerAssetOwnerScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "asset_owner_bindings",
                schema: "media",
                columns: table => new
                {
                    media_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_owner_bindings", x => x.media_asset_id);
                });

            migrationBuilder.CreateTable(
                name: "asset_owner_occurrences",
                schema: "media",
                columns: table => new
                {
                    occurrence_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_asset_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_owner_occurrences", x => x.occurrence_key);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asset_owner_bindings_tenant_id_seller_party_id_created_at",
                schema: "media",
                table: "asset_owner_bindings",
                columns: new[] { "tenant_id", "seller_party_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_owner_occurrences_tenant_id_seller_party_id_created_at",
                schema: "media",
                table: "asset_owner_occurrences",
                columns: new[] { "tenant_id", "seller_party_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_owner_bindings",
                schema: "media");

            migrationBuilder.DropTable(
                name: "asset_owner_occurrences",
                schema: "media");
        }
    }
}
