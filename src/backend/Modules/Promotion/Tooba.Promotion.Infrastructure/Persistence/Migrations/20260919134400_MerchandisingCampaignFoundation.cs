using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Promotion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MerchandisingCampaignFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "merchandising_promotion_types",
                schema: "promotion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_merchandising_promotion_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "merchandising_promotion_type_translations",
                schema: "promotion",
                columns: table => new
                {
                    type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_merchandising_promotion_type_translations", x => new { x.type_id, x.locale });
                    table.ForeignKey(
                        name: "fk_merchandising_promotion_type_translations_merchandising_pro",
                        column: x => x.type_id,
                        principalSchema: "promotion",
                        principalTable: "merchandising_promotion_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "merchandising_campaigns",
                schema: "promotion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    promotion_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lifecycle_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    start_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_merchandising_campaigns", x => x.id);
                    table.ForeignKey(
                        name: "fk_merchandising_campaigns_merchandising_promotion_types_promo",
                        column: x => x.promotion_type_id,
                        principalSchema: "promotion",
                        principalTable: "merchandising_promotion_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "merchandising_campaign_offers",
                schema: "promotion",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    seller_offer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_merchandising_campaign_offers", x => x.id);
                    table.ForeignKey(
                        name: "fk_merchandising_campaign_offers_merchandising_campaigns_campa",
                        column: x => x.campaign_id,
                        principalSchema: "promotion",
                        principalTable: "merchandising_campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "merchandising_campaign_translations",
                schema: "promotion",
                columns: table => new
                {
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    subtitle = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    badge_text = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_merchandising_campaign_translations", x => new { x.campaign_id, x.locale });
                    table.ForeignKey(
                        name: "fk_merchandising_campaign_translations_merchandising_campaigns",
                        column: x => x.campaign_id,
                        principalSchema: "promotion",
                        principalTable: "merchandising_campaigns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_merchandising_campaign_offers_campaign_id_seller_offer_id",
                schema: "promotion",
                table: "merchandising_campaign_offers",
                columns: new[] { "campaign_id", "seller_offer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_merchandising_campaign_offers_campaign_id_sort_order",
                schema: "promotion",
                table: "merchandising_campaign_offers",
                columns: new[] { "campaign_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_merchandising_campaign_offers_seller_offer_id",
                schema: "promotion",
                table: "merchandising_campaign_offers",
                column: "seller_offer_id");

            migrationBuilder.CreateIndex(
                name: "ix_merchandising_campaigns_lifecycle_status_start_at_end_at",
                schema: "promotion",
                table: "merchandising_campaigns",
                columns: new[] { "lifecycle_status", "start_at", "end_at" });

            migrationBuilder.CreateIndex(
                name: "ix_merchandising_campaigns_promotion_type_id",
                schema: "promotion",
                table: "merchandising_campaigns",
                column: "promotion_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_merchandising_campaigns_store_id",
                schema: "promotion",
                table: "merchandising_campaigns",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "ix_merchandising_campaigns_store_id_priority",
                schema: "promotion",
                table: "merchandising_campaigns",
                columns: new[] { "store_id", "priority" });

            migrationBuilder.CreateIndex(
                name: "ix_merchandising_campaigns_store_id_promotion_type_id_lifecycl",
                schema: "promotion",
                table: "merchandising_campaigns",
                columns: new[] { "store_id", "promotion_type_id", "lifecycle_status" });

            migrationBuilder.CreateIndex(
                name: "ix_merchandising_promotion_types_code",
                schema: "promotion",
                table: "merchandising_promotion_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_merchandising_promotion_types_is_active_sort_order",
                schema: "promotion",
                table: "merchandising_promotion_types",
                columns: new[] { "is_active", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "merchandising_campaign_offers",
                schema: "promotion");

            migrationBuilder.DropTable(
                name: "merchandising_campaign_translations",
                schema: "promotion");

            migrationBuilder.DropTable(
                name: "merchandising_promotion_type_translations",
                schema: "promotion");

            migrationBuilder.DropTable(
                name: "merchandising_campaigns",
                schema: "promotion");

            migrationBuilder.DropTable(
                name: "merchandising_promotion_types",
                schema: "promotion");
        }
    }
}
