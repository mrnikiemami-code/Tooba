using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMegaMenuItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mega_menu_items",
                schema: "catalog",
                columns: table => new
                {
                    mega_menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_mega_menu_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    image_media_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    icon_media_asset_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mega_menu_items", x => x.mega_menu_item_id);
                    table.ForeignKey(
                        name: "fk_mega_menu_items_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_mega_menu_items_mega_menu_items_parent_mega_menu_item_id",
                        column: x => x.parent_mega_menu_item_id,
                        principalSchema: "catalog",
                        principalTable: "mega_menu_items",
                        principalColumn: "mega_menu_item_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "mega_menu_item_translations",
                schema: "catalog",
                columns: table => new
                {
                    mega_menu_item_translation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mega_menu_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    locale = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    title_override = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    badge_text = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    short_label = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mega_menu_item_translations", x => x.mega_menu_item_translation_id);
                    table.ForeignKey(
                        name: "fk_mega_menu_item_translations_mega_menu_items_mega_menu_item_id",
                        column: x => x.mega_menu_item_id,
                        principalSchema: "catalog",
                        principalTable: "mega_menu_items",
                        principalColumn: "mega_menu_item_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_mega_menu_items_category_id",
                schema: "catalog",
                table: "mega_menu_items",
                column: "category_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mega_menu_items_parent_mega_menu_item_id_sort_order",
                schema: "catalog",
                table: "mega_menu_items",
                columns: new[] { "parent_mega_menu_item_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_mega_menu_item_translations_mega_menu_item_id_locale",
                schema: "catalog",
                table: "mega_menu_item_translations",
                columns: new[] { "mega_menu_item_id", "locale" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mega_menu_item_translations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "mega_menu_items",
                schema: "catalog");
        }
    }
}
