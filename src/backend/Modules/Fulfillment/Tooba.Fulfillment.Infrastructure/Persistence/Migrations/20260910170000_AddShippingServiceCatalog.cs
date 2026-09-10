using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Fulfillment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Fulfillment.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(FulfillmentDbContext))]
    [Migration("20260910170000_AddShippingServiceCatalog")]
    public partial class AddShippingServiceCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS fulfillment.shipping_services
                (
                    shipping_service_id uuid NOT NULL,
                    code character varying(64) NOT NULL,
                    provider_kind character varying(64) NOT NULL,
                    icon_key character varying(32) NOT NULL,
                    color_key character varying(32) NOT NULL,
                    sort_order integer NOT NULL,
                    is_active boolean NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_shipping_services PRIMARY KEY (shipping_service_id)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_shipping_services_code
                    ON fulfillment.shipping_services (code);

                CREATE TABLE IF NOT EXISTS fulfillment.shipping_service_translations
                (
                    translation_id uuid NOT NULL,
                    shipping_service_id uuid NOT NULL,
                    language_id uuid NOT NULL,
                    name character varying(128) NOT NULL,
                    description character varying(512) NULL,
                    CONSTRAINT pk_shipping_service_translations PRIMARY KEY (translation_id)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_shipping_service_translations_service_lang
                    ON fulfillment.shipping_service_translations (shipping_service_id, language_id);

                CREATE TABLE IF NOT EXISTS fulfillment.shipping_service_options
                (
                    shipping_service_option_id uuid NOT NULL,
                    shipping_service_id uuid NOT NULL,
                    code character varying(64) NOT NULL,
                    sort_order integer NOT NULL,
                    is_active boolean NOT NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_shipping_service_options PRIMARY KEY (shipping_service_option_id)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_shipping_service_options_service_code
                    ON fulfillment.shipping_service_options (shipping_service_id, code);

                CREATE INDEX IF NOT EXISTS ix_shipping_service_options_service_id
                    ON fulfillment.shipping_service_options (shipping_service_id);

                CREATE TABLE IF NOT EXISTS fulfillment.shipping_service_option_translations
                (
                    translation_id uuid NOT NULL,
                    shipping_service_option_id uuid NOT NULL,
                    language_id uuid NOT NULL,
                    name character varying(128) NOT NULL,
                    CONSTRAINT pk_shipping_service_option_translations PRIMARY KEY (translation_id)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ix_shipping_service_option_translations_option_lang
                    ON fulfillment.shipping_service_option_translations (shipping_service_option_id, language_id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS fulfillment.shipping_service_option_translations;
                DROP TABLE IF EXISTS fulfillment.shipping_service_options;
                DROP TABLE IF EXISTS fulfillment.shipping_service_translations;
                DROP TABLE IF EXISTS fulfillment.shipping_services;
                """);
        }
    }
}
