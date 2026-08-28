using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryFacetConfigurations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "category_facet_configurations",
                schema: "catalog",
                columns: table => new
                {
                    facet_configuration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_visible = table.Column<bool>(type: "boolean", nullable: false),
                    is_searchable = table.Column<bool>(type: "boolean", nullable: false),
                    is_collapsed_by_default = table.Column<bool>(type: "boolean", nullable: false),
                    show_counts = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_category_facet_configurations", x => x.facet_configuration_id);
                    table.ForeignKey(
                        name: "fk_category_facet_configurations_attribute_definitions_definitio",
                        column: x => x.definition_id,
                        principalSchema: "catalog",
                        principalTable: "attribute_definitions",
                        principalColumn: "definition_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_category_facet_configurations_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_category_facet_configurations_category_id_definition_id",
                schema: "catalog",
                table: "category_facet_configurations",
                columns: new[] { "category_id", "definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_category_facet_configurations_definition_id",
                schema: "catalog",
                table: "category_facet_configurations",
                column: "definition_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "category_facet_configurations",
                schema: "catalog");
        }
    }
}
