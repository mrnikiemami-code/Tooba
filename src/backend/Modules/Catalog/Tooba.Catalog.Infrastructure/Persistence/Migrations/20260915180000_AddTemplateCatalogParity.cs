using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T022-R6 — complete Template Catalog structural parity mirrors.</summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("20260915180000_AddTemplateCatalogParity")]
public partial class AddTemplateCatalogParity : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS catalog.template_tags (
                tag_id uuid NOT NULL,
                template_id uuid NOT NULL,
                code character varying(64) NOT NULL,
                slug_seam character varying(128) NULL,
                status character varying(32) NOT NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_template_tags PRIMARY KEY (tag_id),
                CONSTRAINT fk_template_tags_template FOREIGN KEY (template_id)
                    REFERENCES catalog.store_templates (template_id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_template_tags_template_id ON catalog.template_tags (template_id);
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_tags_template_code ON catalog.template_tags (template_id, code);

            CREATE TABLE IF NOT EXISTS catalog.template_attribute_definitions (
                definition_id uuid NOT NULL,
                template_id uuid NOT NULL,
                code character varying(64) NOT NULL,
                value_kind character varying(32) NOT NULL,
                is_variant_axis boolean NOT NULL,
                unit character varying(32) NULL,
                is_required boolean NOT NULL,
                is_filterable boolean NOT NULL,
                is_comparable boolean NOT NULL,
                is_multivalue boolean NOT NULL,
                display_order integer NOT NULL,
                validation_min numeric(18,4) NULL,
                validation_max numeric(18,4) NULL,
                validation_max_length integer NULL,
                is_active boolean NOT NULL,
                created_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_template_attribute_definitions PRIMARY KEY (definition_id),
                CONSTRAINT fk_template_attribute_definitions_template FOREIGN KEY (template_id)
                    REFERENCES catalog.store_templates (template_id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_template_attribute_definitions_template_id
                ON catalog.template_attribute_definitions (template_id);
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_attribute_definitions_template_code
                ON catalog.template_attribute_definitions (template_id, code);

            CREATE TABLE IF NOT EXISTS catalog.template_attribute_options (
                option_id uuid NOT NULL,
                definition_id uuid NOT NULL,
                code character varying(64) NOT NULL,
                display_order integer NOT NULL,
                is_active boolean NOT NULL,
                CONSTRAINT pk_template_attribute_options PRIMARY KEY (option_id),
                CONSTRAINT fk_template_attribute_options_definition FOREIGN KEY (definition_id)
                    REFERENCES catalog.template_attribute_definitions (definition_id) ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_attribute_options_definition_code
                ON catalog.template_attribute_options (definition_id, code);

            CREATE TABLE IF NOT EXISTS catalog.template_product_tag_assignments (
                assignment_id uuid NOT NULL,
                product_id uuid NOT NULL,
                tag_id uuid NOT NULL,
                CONSTRAINT pk_template_product_tag_assignments PRIMARY KEY (assignment_id),
                CONSTRAINT fk_template_product_tag_assignments_product FOREIGN KEY (product_id)
                    REFERENCES catalog.template_products (product_id) ON DELETE CASCADE,
                CONSTRAINT fk_template_product_tag_assignments_tag FOREIGN KEY (tag_id)
                    REFERENCES catalog.template_tags (tag_id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_product_tag_assignments_product_tag
                ON catalog.template_product_tag_assignments (product_id, tag_id);

            CREATE TABLE IF NOT EXISTS catalog.template_category_tag_assignments (
                assignment_id uuid NOT NULL,
                category_id uuid NOT NULL,
                tag_id uuid NOT NULL,
                CONSTRAINT pk_template_category_tag_assignments PRIMARY KEY (assignment_id),
                CONSTRAINT fk_template_category_tag_assignments_category FOREIGN KEY (category_id)
                    REFERENCES catalog.template_categories (category_id) ON DELETE CASCADE,
                CONSTRAINT fk_template_category_tag_assignments_tag FOREIGN KEY (tag_id)
                    REFERENCES catalog.template_tags (tag_id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_category_tag_assignments_category_tag
                ON catalog.template_category_tag_assignments (category_id, tag_id);

            CREATE TABLE IF NOT EXISTS catalog.template_category_attribute_bindings (
                binding_id uuid NOT NULL,
                category_id uuid NOT NULL,
                definition_id uuid NOT NULL,
                display_order integer NOT NULL,
                is_required boolean NOT NULL,
                is_filterable boolean NOT NULL,
                is_variant_axis boolean NOT NULL,
                is_comparable boolean NOT NULL,
                created_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_template_category_attribute_bindings PRIMARY KEY (binding_id),
                CONSTRAINT fk_template_category_attribute_bindings_category FOREIGN KEY (category_id)
                    REFERENCES catalog.template_categories (category_id) ON DELETE CASCADE,
                CONSTRAINT fk_template_category_attribute_bindings_definition FOREIGN KEY (definition_id)
                    REFERENCES catalog.template_attribute_definitions (definition_id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_category_attribute_bindings_category_definition
                ON catalog.template_category_attribute_bindings (category_id, definition_id);

            CREATE TABLE IF NOT EXISTS catalog.template_category_facet_configurations (
                facet_configuration_id uuid NOT NULL,
                category_id uuid NOT NULL,
                definition_id uuid NOT NULL,
                display_type character varying(32) NOT NULL,
                sort_order integer NOT NULL,
                is_visible boolean NOT NULL,
                is_searchable boolean NOT NULL,
                is_collapsed_by_default boolean NOT NULL,
                show_counts boolean NOT NULL,
                created_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_template_category_facet_configurations PRIMARY KEY (facet_configuration_id),
                CONSTRAINT fk_template_category_facet_configurations_category FOREIGN KEY (category_id)
                    REFERENCES catalog.template_categories (category_id) ON DELETE CASCADE,
                CONSTRAINT fk_template_category_facet_configurations_definition FOREIGN KEY (definition_id)
                    REFERENCES catalog.template_attribute_definitions (definition_id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_category_facet_configurations_category_definition
                ON catalog.template_category_facet_configurations (category_id, definition_id);

            CREATE TABLE IF NOT EXISTS catalog.template_mega_menu_items (
                mega_menu_item_id uuid NOT NULL,
                item_type character varying(32) NOT NULL,
                category_id uuid NOT NULL,
                parent_mega_menu_item_id uuid NULL,
                sort_order integer NOT NULL,
                is_visible boolean NOT NULL,
                is_featured boolean NOT NULL,
                image_media_asset_id uuid NULL,
                icon_media_asset_id uuid NULL,
                created_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_template_mega_menu_items PRIMARY KEY (mega_menu_item_id),
                CONSTRAINT fk_template_mega_menu_items_category FOREIGN KEY (category_id)
                    REFERENCES catalog.template_categories (category_id) ON DELETE CASCADE,
                CONSTRAINT fk_template_mega_menu_items_parent FOREIGN KEY (parent_mega_menu_item_id)
                    REFERENCES catalog.template_mega_menu_items (mega_menu_item_id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_mega_menu_items_category_id
                ON catalog.template_mega_menu_items (category_id);
            CREATE INDEX IF NOT EXISTS ix_template_mega_menu_items_parent_sort
                ON catalog.template_mega_menu_items (parent_mega_menu_item_id, sort_order);

            CREATE TABLE IF NOT EXISTS catalog.template_mega_menu_item_translations (
                mega_menu_item_translation_id uuid NOT NULL,
                mega_menu_item_id uuid NOT NULL,
                locale character varying(32) NOT NULL,
                title_override character varying(256) NULL,
                badge_text character varying(64) NULL,
                short_label character varying(128) NULL,
                CONSTRAINT pk_template_mega_menu_item_translations PRIMARY KEY (mega_menu_item_translation_id),
                CONSTRAINT fk_template_mega_menu_item_translations_item FOREIGN KEY (mega_menu_item_id)
                    REFERENCES catalog.template_mega_menu_items (mega_menu_item_id) ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_mega_menu_item_translations_item_locale
                ON catalog.template_mega_menu_item_translations (mega_menu_item_id, locale);

            CREATE TABLE IF NOT EXISTS catalog.template_product_variant_axes (
                axis_id uuid NOT NULL,
                product_id uuid NOT NULL,
                definition_id uuid NOT NULL,
                display_order integer NOT NULL,
                CONSTRAINT pk_template_product_variant_axes PRIMARY KEY (axis_id),
                CONSTRAINT fk_template_product_variant_axes_product FOREIGN KEY (product_id)
                    REFERENCES catalog.template_products (product_id) ON DELETE CASCADE,
                CONSTRAINT fk_template_product_variant_axes_definition FOREIGN KEY (definition_id)
                    REFERENCES catalog.template_attribute_definitions (definition_id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_product_variant_axes_product_definition
                ON catalog.template_product_variant_axes (product_id, definition_id);

            CREATE TABLE IF NOT EXISTS catalog.template_product_attribute_values (
                value_id uuid NOT NULL,
                product_id uuid NOT NULL,
                definition_id uuid NOT NULL,
                canonical_value character varying(256) NOT NULL,
                CONSTRAINT pk_template_product_attribute_values PRIMARY KEY (value_id),
                CONSTRAINT fk_template_product_attribute_values_product FOREIGN KEY (product_id)
                    REFERENCES catalog.template_products (product_id) ON DELETE CASCADE,
                CONSTRAINT fk_template_product_attribute_values_definition FOREIGN KEY (definition_id)
                    REFERENCES catalog.template_attribute_definitions (definition_id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_product_attribute_values_product_definition
                ON catalog.template_product_attribute_values (product_id, definition_id);

            CREATE TABLE IF NOT EXISTS catalog.template_variant_attribute_values (
                value_id uuid NOT NULL,
                variant_id uuid NOT NULL,
                definition_id uuid NOT NULL,
                canonical_value character varying(256) NOT NULL,
                CONSTRAINT pk_template_variant_attribute_values PRIMARY KEY (value_id),
                CONSTRAINT fk_template_variant_attribute_values_variant FOREIGN KEY (variant_id)
                    REFERENCES catalog.template_variants (variant_id) ON DELETE CASCADE,
                CONSTRAINT fk_template_variant_attribute_values_definition FOREIGN KEY (definition_id)
                    REFERENCES catalog.template_attribute_definitions (definition_id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_variant_attribute_values_variant_definition
                ON catalog.template_variant_attribute_values (variant_id, definition_id);

            CREATE TABLE IF NOT EXISTS catalog.template_product_history_entries (
                history_id uuid NOT NULL,
                product_id uuid NOT NULL,
                event_type character varying(64) NOT NULL,
                section character varying(32) NOT NULL,
                summary_fa character varying(512) NOT NULL,
                before_summary character varying(512) NULL,
                after_summary character varying(512) NULL,
                actor_user_id uuid NULL,
                actor_display_name character varying(256) NULL,
                occurred_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_template_product_history_entries PRIMARY KEY (history_id),
                CONSTRAINT fk_template_product_history_entries_product FOREIGN KEY (product_id)
                    REFERENCES catalog.template_products (product_id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_template_product_history_entries_product_occurred
                ON catalog.template_product_history_entries (product_id, occurred_at);

            CREATE TABLE IF NOT EXISTS catalog.template_category_slug_histories (
                history_id uuid NOT NULL,
                category_id uuid NOT NULL,
                locale character varying(32) NOT NULL,
                old_slug character varying(160) NOT NULL,
                changed_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_template_category_slug_histories PRIMARY KEY (history_id),
                CONSTRAINT fk_template_category_slug_histories_category FOREIGN KEY (category_id)
                    REFERENCES catalog.template_categories (category_id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_template_category_slug_histories_locale_old_slug
                ON catalog.template_category_slug_histories (locale, old_slug);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS catalog.template_category_slug_histories;
            DROP TABLE IF EXISTS catalog.template_product_history_entries;
            DROP TABLE IF EXISTS catalog.template_variant_attribute_values;
            DROP TABLE IF EXISTS catalog.template_product_attribute_values;
            DROP TABLE IF EXISTS catalog.template_product_variant_axes;
            DROP TABLE IF EXISTS catalog.template_mega_menu_item_translations;
            DROP TABLE IF EXISTS catalog.template_mega_menu_items;
            DROP TABLE IF EXISTS catalog.template_category_facet_configurations;
            DROP TABLE IF EXISTS catalog.template_category_attribute_bindings;
            DROP TABLE IF EXISTS catalog.template_category_tag_assignments;
            DROP TABLE IF EXISTS catalog.template_product_tag_assignments;
            DROP TABLE IF EXISTS catalog.template_attribute_options;
            DROP TABLE IF EXISTS catalog.template_attribute_definitions;
            DROP TABLE IF EXISTS catalog.template_tags;
            """);
    }
}
