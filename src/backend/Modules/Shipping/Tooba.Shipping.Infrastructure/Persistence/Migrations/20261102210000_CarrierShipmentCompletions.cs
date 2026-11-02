using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Shipping.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations;

/// <summary>TB-249-R1: durable sender/package completions for carrier execution (not Order money).</summary>
[DbContext(typeof(ShippingDbContext))]
[Migration("20261102210000_CarrierShipmentCompletions")]
public partial class CarrierShipmentCompletions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            CREATE TABLE shipping.carrier_shipment_sender_completions (
                completion_id uuid NOT NULL,
                owner_kind integer NOT NULL,
                owner_tenant_id character varying(128),
                owner_seller_party_id uuid,
                checkout_id uuid NOT NULL,
                shipping_group_id uuid NOT NULL,
                full_name character varying(256) NOT NULL,
                mobile character varying(32) NOT NULL,
                address_line character varying(512) NOT NULL,
                postal_code character varying(32),
                location_code character varying(64) NOT NULL,
                completed_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_carrier_shipment_sender_completions PRIMARY KEY (completion_id),
                CONSTRAINT ck_carrier_shipment_sender_completions_owner_kind CHECK (owner_kind IN (0, 1, 2))
            );
            CREATE UNIQUE INDEX ux_carrier_shipment_sender_completions_group
                ON shipping.carrier_shipment_sender_completions (owner_kind, owner_tenant_id, owner_seller_party_id, checkout_id, shipping_group_id);

            CREATE TABLE shipping.carrier_shipment_package_completions (
                completion_id uuid NOT NULL,
                owner_kind integer NOT NULL,
                owner_tenant_id character varying(128),
                owner_seller_party_id uuid,
                checkout_id uuid NOT NULL,
                shipping_group_id uuid NOT NULL,
                weight_grams numeric(18,2) NOT NULL,
                declared_value numeric(18,2) NOT NULL,
                length_centimeters numeric(18,2) NOT NULL,
                width_centimeters numeric(18,2) NOT NULL,
                height_centimeters numeric(18,2) NOT NULL,
                completed_at timestamp with time zone NOT NULL,
                CONSTRAINT pk_carrier_shipment_package_completions PRIMARY KEY (completion_id),
                CONSTRAINT ck_carrier_shipment_package_completions_owner_kind CHECK (owner_kind IN (0, 1, 2))
            );
            CREATE UNIQUE INDEX ux_carrier_shipment_package_completions_group
                ON shipping.carrier_shipment_package_completions (owner_kind, owner_tenant_id, owner_seller_party_id, checkout_id, shipping_group_id);
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DROP TABLE IF EXISTS shipping.carrier_shipment_package_completions;
            DROP TABLE IF EXISTS shipping.carrier_shipment_sender_completions;
            """);
    }
}
