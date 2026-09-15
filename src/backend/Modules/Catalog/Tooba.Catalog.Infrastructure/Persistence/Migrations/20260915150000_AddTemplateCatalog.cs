using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Catalog.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Catalog.Infrastructure.Persistence.Migrations;

/// <summary>TB-P10-T022-R5 — Template Catalog mirror tables + TemplateId indexes.</summary>
[DbContext(typeof(CatalogDbContext))]
[Migration("20260915150000_AddTemplateCatalog")]
public partial class AddTemplateCatalog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE IF NOT EXISTS catalog.store_templates (
                template_id uuid NOT NULL,
                key character varying(64) NOT NULL,
                name character varying(256) NOT NULL,
                is_active boolean NOT NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_store_templates PRIMARY KEY (template_id)
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_store_templates_key ON catalog.store_templates (key);

            CREATE TABLE IF NOT EXISTS catalog.template_categories (
                category_id uuid NOT NULL,
                template_id uuid NOT NULL,
                parent_category_id uuid NULL,
                status character varying(32) NOT NULL,
                sort_order integer NOT NULL DEFAULT 0,
                is_visible boolean NOT NULL DEFAULT TRUE,
                image_media_asset_id uuid NULL,
                icon_media_asset_id uuid NULL,
                banner_media_asset_id uuid NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_template_categories PRIMARY KEY (category_id),
                CONSTRAINT fk_template_categories_template FOREIGN KEY (template_id)
                    REFERENCES catalog.store_templates (template_id) ON DELETE CASCADE,
                CONSTRAINT fk_template_categories_parent FOREIGN KEY (parent_category_id)
                    REFERENCES catalog.template_categories (category_id) ON DELETE RESTRICT
            );
            CREATE INDEX IF NOT EXISTS ix_template_categories_template_id
                ON catalog.template_categories (template_id);
            CREATE INDEX IF NOT EXISTS ix_template_categories_template_parent_sort
                ON catalog.template_categories (template_id, parent_category_id, sort_order);

            CREATE TABLE IF NOT EXISTS catalog.template_category_translations (
                translation_id uuid NOT NULL,
                category_id uuid NOT NULL,
                locale character varying(32) NOT NULL,
                name character varying(256) NOT NULL,
                slug character varying(160) NOT NULL,
                short_description character varying(512) NULL,
                description character varying(4000) NULL,
                seo_title character varying(256) NULL,
                seo_description character varying(512) NULL,
                meta_keywords character varying(512) NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_template_category_translations PRIMARY KEY (translation_id),
                CONSTRAINT fk_template_category_translations_category FOREIGN KEY (category_id)
                    REFERENCES catalog.template_categories (category_id) ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_category_translations_category_locale
                ON catalog.template_category_translations (category_id, locale);
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_category_translations_locale_slug
                ON catalog.template_category_translations (locale, slug);

            CREATE TABLE IF NOT EXISTS catalog.template_brands (
                brand_id uuid NOT NULL,
                template_id uuid NOT NULL,
                slug_seam character varying(128) NULL,
                status character varying(32) NOT NULL,
                logo_media_asset_id uuid NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_template_brands PRIMARY KEY (brand_id),
                CONSTRAINT fk_template_brands_template FOREIGN KEY (template_id)
                    REFERENCES catalog.store_templates (template_id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_template_brands_template_id
                ON catalog.template_brands (template_id);

            CREATE TABLE IF NOT EXISTS catalog.template_localized_texts (
                text_id uuid NOT NULL,
                owner_kind character varying(32) NOT NULL,
                owner_id uuid NOT NULL,
                field_key character varying(64) NOT NULL,
                locale character varying(32) NOT NULL,
                value character varying(1024) NOT NULL,
                CONSTRAINT pk_template_localized_texts PRIMARY KEY (text_id)
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_localized_texts_owner_field_locale
                ON catalog.template_localized_texts (owner_kind, owner_id, field_key, locale);

            CREATE TABLE IF NOT EXISTS catalog.template_products (
                product_id uuid NOT NULL,
                template_id uuid NOT NULL,
                kind character varying(32) NOT NULL,
                status character varying(32) NOT NULL,
                brand_id uuid NULL,
                slug_seam character varying(160) NULL,
                seo_title_seam character varying(256) NULL,
                unit_of_measure_id uuid NOT NULL,
                quantity_decimal_places integer NOT NULL DEFAULT 0,
                quantity_step numeric(18,6) NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_template_products PRIMARY KEY (product_id),
                CONSTRAINT fk_template_products_template FOREIGN KEY (template_id)
                    REFERENCES catalog.store_templates (template_id) ON DELETE CASCADE,
                CONSTRAINT fk_template_products_brand FOREIGN KEY (brand_id)
                    REFERENCES catalog.template_brands (brand_id) ON DELETE SET NULL
            );
            CREATE INDEX IF NOT EXISTS ix_template_products_template_id
                ON catalog.template_products (template_id);

            CREATE TABLE IF NOT EXISTS catalog.template_product_categories (
                assignment_id uuid NOT NULL,
                product_id uuid NOT NULL,
                category_id uuid NOT NULL,
                role smallint NOT NULL DEFAULT 0,
                CONSTRAINT pk_template_product_categories PRIMARY KEY (assignment_id),
                CONSTRAINT fk_template_product_categories_product FOREIGN KEY (product_id)
                    REFERENCES catalog.template_products (product_id) ON DELETE CASCADE,
                CONSTRAINT fk_template_product_categories_category FOREIGN KEY (category_id)
                    REFERENCES catalog.template_categories (category_id) ON DELETE RESTRICT
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_product_categories_product_category
                ON catalog.template_product_categories (product_id, category_id);
            CREATE INDEX IF NOT EXISTS ix_template_product_categories_category_role
                ON catalog.template_product_categories (category_id, role);
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_product_categories_one_primary_per_product
                ON catalog.template_product_categories (product_id) WHERE role = 0;

            CREATE TABLE IF NOT EXISTS catalog.template_product_media_references (
                reference_id uuid NOT NULL,
                product_id uuid NOT NULL,
                media_asset_id uuid NOT NULL,
                display_order integer NOT NULL DEFAULT 0,
                is_primary boolean NOT NULL DEFAULT FALSE,
                alt_text character varying(512) NULL,
                CONSTRAINT pk_template_product_media_references PRIMARY KEY (reference_id),
                CONSTRAINT fk_template_product_media_references_product FOREIGN KEY (product_id)
                    REFERENCES catalog.template_products (product_id) ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_product_media_references_product_media
                ON catalog.template_product_media_references (product_id, media_asset_id);
            CREATE INDEX IF NOT EXISTS ix_template_product_media_references_product_order
                ON catalog.template_product_media_references (product_id, display_order);

            CREATE TABLE IF NOT EXISTS catalog.template_variants (
                variant_id uuid NOT NULL,
                product_id uuid NOT NULL,
                catalog_code_seam character varying(64) NULL,
                combination_fingerprint character varying(512) NOT NULL,
                status character varying(32) NOT NULL,
                sort_order integer NOT NULL DEFAULT 0,
                is_default boolean NOT NULL DEFAULT FALSE,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_template_variants PRIMARY KEY (variant_id),
                CONSTRAINT fk_template_variants_product FOREIGN KEY (product_id)
                    REFERENCES catalog.template_products (product_id) ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_variants_product_fingerprint
                ON catalog.template_variants (product_id, combination_fingerprint);

            CREATE TABLE IF NOT EXISTS catalog.template_store_landing_pages (
                page_id uuid NOT NULL,
                template_id uuid NOT NULL,
                locale character varying(16) NOT NULL,
                slug character varying(128) NOT NULL,
                title character varying(200) NOT NULL,
                seo_title character varying(200) NULL,
                seo_description character varying(500) NULL,
                template_key character varying(32) NOT NULL,
                status character varying(16) NOT NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_template_store_landing_pages PRIMARY KEY (page_id),
                CONSTRAINT fk_template_store_landing_pages_template FOREIGN KEY (template_id)
                    REFERENCES catalog.store_templates (template_id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_template_store_landing_pages_template_id
                ON catalog.template_store_landing_pages (template_id);
            CREATE UNIQUE INDEX IF NOT EXISTS ix_template_store_landing_pages_template_locale_slug
                ON catalog.template_store_landing_pages (template_id, locale, slug);

            CREATE TABLE IF NOT EXISTS catalog.template_store_landing_page_sections (
                page_section_id uuid NOT NULL,
                page_id uuid NOT NULL,
                section_type character varying(64) NOT NULL,
                sort_order integer NOT NULL,
                is_enabled boolean NOT NULL,
                configuration_json character varying(12000) NOT NULL,
                created_at timestamp with time zone NOT NULL,
                updated_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_template_store_landing_page_sections PRIMARY KEY (page_section_id),
                CONSTRAINT fk_template_store_landing_page_sections_page FOREIGN KEY (page_id)
                    REFERENCES catalog.template_store_landing_pages (page_id) ON DELETE CASCADE
            );
            CREATE INDEX IF NOT EXISTS ix_template_store_landing_page_sections_page_sort
                ON catalog.template_store_landing_page_sections (page_id, sort_order);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS catalog.template_store_landing_page_sections;
            DROP TABLE IF EXISTS catalog.template_store_landing_pages;
            DROP TABLE IF EXISTS catalog.template_variants;
            DROP TABLE IF EXISTS catalog.template_product_media_references;
            DROP TABLE IF EXISTS catalog.template_product_categories;
            DROP TABLE IF EXISTS catalog.template_products;
            DROP TABLE IF EXISTS catalog.template_localized_texts;
            DROP TABLE IF EXISTS catalog.template_brands;
            DROP TABLE IF EXISTS catalog.template_category_translations;
            DROP TABLE IF EXISTS catalog.template_categories;
            DROP TABLE IF EXISTS catalog.store_templates;
            """);
    }
}
