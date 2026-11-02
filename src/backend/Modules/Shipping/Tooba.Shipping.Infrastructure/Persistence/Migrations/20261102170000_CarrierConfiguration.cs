using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-248: owner-scoped carrier accounts, secret material references, provider reference catalog,
    /// geography→carrier destination mappings, and draft CarrierApi method bindings.
    /// </summary>
    public partial class CarrierConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE shipping.carrier_accounts (
                    account_id uuid NOT NULL,
                    owner_kind integer NOT NULL,
                    owner_tenant_id character varying(128) NULL,
                    owner_seller_party_id uuid NULL,
                    provider_code character varying(64) NOT NULL,
                    display_name character varying(128) NOT NULL,
                    environment integer NOT NULL,
                    secret_reference character varying(128) NOT NULL,
                    is_active boolean NOT NULL,
                    configuration_version integer NOT NULL,
                    api_base_url character varying(256) NULL,
                    token_url character varying(256) NULL,
                    currency_code character varying(3) NOT NULL,
                    last_connection_outcome integer NULL,
                    last_connection_at timestamp with time zone NULL,
                    last_reference_sync_at timestamp with time zone NULL,
                    created_at timestamp with time zone NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_carrier_accounts PRIMARY KEY (account_id),
                    CONSTRAINT ck_carrier_accounts_owner_kind CHECK (owner_kind IN (0, 1, 2)),
                    CONSTRAINT ck_carrier_accounts_environment CHECK (environment IN (0, 1)),
                    CONSTRAINT ck_carrier_accounts_configuration_version CHECK (configuration_version >= 1),
                    CONSTRAINT ux_carrier_accounts_secret_reference UNIQUE (secret_reference)
                );

                CREATE UNIQUE INDEX ux_carrier_accounts_owner_provider_display
                    ON shipping.carrier_accounts (owner_kind, owner_tenant_id, owner_seller_party_id, provider_code, display_name)
                    NULLS NOT DISTINCT;

                CREATE TABLE shipping.carrier_secret_materials (
                    secret_reference character varying(128) NOT NULL,
                    ciphertext bytea NOT NULL,
                    configuration_version integer NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_carrier_secret_materials PRIMARY KEY (secret_reference),
                    CONSTRAINT ck_carrier_secret_materials_configuration_version CHECK (configuration_version >= 1)
                );

                CREATE TABLE shipping.carrier_reference_values (
                    account_id uuid NOT NULL,
                    kind integer NOT NULL,
                    external_id character varying(64) NOT NULL,
                    title character varying(256) NOT NULL,
                    is_active boolean NOT NULL,
                    synced_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_carrier_reference_values PRIMARY KEY (account_id, kind, external_id),
                    CONSTRAINT fk_carrier_reference_values_account
                        FOREIGN KEY (account_id) REFERENCES shipping.carrier_accounts (account_id) ON DELETE CASCADE,
                    CONSTRAINT ck_carrier_reference_values_kind CHECK (kind BETWEEN 0 AND 6)
                );

                CREATE TABLE shipping.carrier_destination_mappings (
                    mapping_id uuid NOT NULL,
                    account_id uuid NOT NULL,
                    provider_code character varying(64) NOT NULL,
                    geography_city_id uuid NOT NULL,
                    external_location_code character varying(64) NOT NULL,
                    provenance character varying(128) NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_carrier_destination_mappings PRIMARY KEY (mapping_id),
                    CONSTRAINT fk_carrier_destination_mappings_account
                        FOREIGN KEY (account_id) REFERENCES shipping.carrier_accounts (account_id) ON DELETE CASCADE,
                    CONSTRAINT ux_carrier_destination_mappings_account_city UNIQUE (account_id, geography_city_id),
                    CONSTRAINT ux_carrier_destination_mappings_account_external UNIQUE (account_id, external_location_code)
                );

                CREATE TABLE shipping.carrier_method_bindings (
                    binding_id uuid NOT NULL,
                    draft_id uuid NOT NULL,
                    carrier_account_id uuid NOT NULL,
                    provider_code character varying(64) NOT NULL,
                    provider_service_id character varying(64) NOT NULL,
                    payment_type_id character varying(64) NOT NULL,
                    pickup_type_id character varying(64) NOT NULL,
                    distribution_type_id character varying(64) NOT NULL,
                    packing_id character varying(64) NOT NULL,
                    package_content_id character varying(64) NOT NULL,
                    pack_type_id character varying(64) NOT NULL,
                    origin_external_city_code character varying(64) NOT NULL,
                    origin_address_line character varying(512) NULL,
                    origin_latitude numeric(9,6) NULL,
                    origin_longitude numeric(9,6) NULL,
                    default_length_centimeters numeric(18,2) NOT NULL,
                    default_width_centimeters numeric(18,2) NOT NULL,
                    default_height_centimeters numeric(18,2) NOT NULL,
                    configuration_version integer NOT NULL,
                    updated_at timestamp with time zone NOT NULL,
                    CONSTRAINT pk_carrier_method_bindings PRIMARY KEY (binding_id),
                    CONSTRAINT ux_carrier_method_bindings_draft UNIQUE (draft_id),
                    CONSTRAINT fk_carrier_method_bindings_account
                        FOREIGN KEY (carrier_account_id) REFERENCES shipping.carrier_accounts (account_id) ON DELETE RESTRICT,
                    CONSTRAINT fk_carrier_method_bindings_draft
                        FOREIGN KEY (draft_id) REFERENCES shipping.authoring_drafts (draft_id) ON DELETE CASCADE,
                    CONSTRAINT ck_carrier_method_bindings_dims CHECK (
                        default_length_centimeters > 0
                        AND default_width_centimeters > 0
                        AND default_height_centimeters > 0),
                    CONSTRAINT ck_carrier_method_bindings_configuration_version CHECK (configuration_version >= 1)
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS shipping.carrier_method_bindings;
                DROP TABLE IF EXISTS shipping.carrier_destination_mappings;
                DROP TABLE IF EXISTS shipping.carrier_reference_values;
                DROP TABLE IF EXISTS shipping.carrier_secret_materials;
                DROP TABLE IF EXISTS shipping.carrier_accounts;
                """);
        }
    }
}
