using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Shipping.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-248-R1: durable carrier shipment operations (create/track/cancel), separate from Order money evidence.
    /// </summary>
    public partial class CarrierShipmentOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE shipping.carrier_shipment_operations (
                    operation_id uuid NOT NULL,
                    operation_key character varying(128) NOT NULL,
                    kind integer NOT NULL,
                    state integer NOT NULL,
                    owner_kind integer NOT NULL,
                    owner_tenant_id character varying(128) NULL,
                    owner_seller_party_id uuid NULL,
                    checkout_id uuid NOT NULL,
                    shipping_group_id uuid NOT NULL,
                    provider_code character varying(64) NOT NULL,
                    account_id uuid NOT NULL,
                    configuration_version integer NOT NULL,
                    provider_tracking_code character varying(64) NULL,
                    provider_order_id character varying(64) NULL,
                    provider_status character varying(128) NULL,
                    last_error_code character varying(128) NULL,
                    attempted_at timestamp with time zone NOT NULL,
                    completed_at timestamp with time zone NULL,
                    CONSTRAINT pk_carrier_shipment_operations PRIMARY KEY (operation_id),
                    CONSTRAINT ck_carrier_shipment_operations_kind CHECK (kind IN (0, 1, 2)),
                    CONSTRAINT ck_carrier_shipment_operations_state CHECK (state IN (0, 1, 2, 3)),
                    CONSTRAINT ck_carrier_shipment_operations_owner_kind CHECK (owner_kind IN (0, 1, 2)),
                    CONSTRAINT ck_carrier_shipment_operations_configuration_version CHECK (configuration_version >= 1),
                    CONSTRAINT ux_carrier_shipment_operations_key UNIQUE (operation_key)
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE shipping.carrier_shipment_operations;");
        }
    }
}
