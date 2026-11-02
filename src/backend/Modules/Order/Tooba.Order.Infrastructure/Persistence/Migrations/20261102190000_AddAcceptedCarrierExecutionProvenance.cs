using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>TB-249: execution provenance on accepted carrier quote evidence (cart + order groups).</summary>
[DbContext(typeof(OrderDbContext))]
[Migration("20261102190000_AddAcceptedCarrierExecutionProvenance")]
public partial class AddAcceptedCarrierExecutionProvenance : Migration
{
    private static readonly string[] Tables = ["cart_shipping_groups", "order_shipping_groups"];

    private static readonly string[] ProvenanceColumns =
    [
        "accepted_carrier_origin_location_code character varying(64) NULL",
        "accepted_carrier_destination_location_code character varying(64) NULL",
        "accepted_carrier_weight_grams numeric(18,2) NULL",
        "accepted_carrier_declared_value numeric(18,2) NULL",
        "accepted_carrier_length_cm numeric(18,2) NULL",
        "accepted_carrier_width_cm numeric(18,2) NULL",
        "accepted_carrier_height_cm numeric(18,2) NULL",
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in Tables)
        {
            foreach (var column in ProvenanceColumns)
            {
                migrationBuilder.Sql($"ALTER TABLE \"order\".{table} ADD COLUMN {column};");
            }

            migrationBuilder.Sql($"ALTER TABLE \"order\".{table} DROP CONSTRAINT ck_{table}_accepted_carrier_quote_complete;");

            // Legacy TB-248-R1 rows may have quote money/identity without execution provenance.
            // Provenance is all-null OR all-complete; execution eligibility is enforced in application.
            migrationBuilder.Sql(
                $"""
                ALTER TABLE "order".{table}
                ADD CONSTRAINT ck_{table}_accepted_carrier_quote_complete CHECK (
                    (accepted_carrier_provider_code IS NULL
                        AND accepted_carrier_account_id IS NULL
                        AND accepted_carrier_configuration_version IS NULL
                        AND accepted_carrier_service_code IS NULL
                        AND accepted_carrier_quote_input_identity IS NULL
                        AND accepted_carrier_quote_currency IS NULL
                        AND accepted_carrier_final_amount IS NULL
                        AND accepted_carrier_transport_amount IS NULL
                        AND accepted_carrier_tax_amount IS NULL
                        AND accepted_carrier_packaging_component IS NULL
                        AND accepted_carrier_pickup_amount IS NULL
                        AND accepted_carrier_delivery_amount IS NULL
                        AND accepted_carrier_issued_at IS NULL
                        AND accepted_carrier_fresh_until IS NULL
                        AND accepted_carrier_origin_location_code IS NULL
                        AND accepted_carrier_destination_location_code IS NULL
                        AND accepted_carrier_weight_grams IS NULL
                        AND accepted_carrier_declared_value IS NULL
                        AND accepted_carrier_length_cm IS NULL
                        AND accepted_carrier_width_cm IS NULL
                        AND accepted_carrier_height_cm IS NULL
                        AND accepted_carrier_provider_reference IS NULL)
                    OR
                    (accepted_carrier_provider_code IS NOT NULL
                        AND accepted_carrier_account_id IS NOT NULL
                        AND accepted_carrier_configuration_version IS NOT NULL
                        AND accepted_carrier_service_code IS NOT NULL
                        AND accepted_carrier_quote_input_identity IS NOT NULL
                        AND accepted_carrier_quote_currency IS NOT NULL
                        AND accepted_carrier_final_amount IS NOT NULL
                        AND accepted_carrier_transport_amount IS NOT NULL
                        AND accepted_carrier_tax_amount IS NOT NULL
                        AND accepted_carrier_packaging_component IS NOT NULL
                        AND accepted_carrier_pickup_amount IS NOT NULL
                        AND accepted_carrier_delivery_amount IS NOT NULL
                        AND accepted_carrier_issued_at IS NOT NULL
                        AND accepted_carrier_fresh_until IS NOT NULL
                        AND (
                            (accepted_carrier_origin_location_code IS NULL
                                AND accepted_carrier_destination_location_code IS NULL
                                AND accepted_carrier_weight_grams IS NULL
                                AND accepted_carrier_declared_value IS NULL
                                AND accepted_carrier_length_cm IS NULL
                                AND accepted_carrier_width_cm IS NULL
                                AND accepted_carrier_height_cm IS NULL)
                            OR
                            (accepted_carrier_origin_location_code IS NOT NULL
                                AND accepted_carrier_destination_location_code IS NOT NULL
                                AND accepted_carrier_weight_grams IS NOT NULL
                                AND accepted_carrier_declared_value IS NOT NULL
                                AND accepted_carrier_length_cm IS NOT NULL
                                AND accepted_carrier_width_cm IS NOT NULL
                                AND accepted_carrier_height_cm IS NOT NULL)
                        ))
                );
                """);
        }
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in Tables)
        {
            migrationBuilder.Sql($"ALTER TABLE \"order\".{table} DROP CONSTRAINT ck_{table}_accepted_carrier_quote_complete;");

            migrationBuilder.Sql(
                $"""
                ALTER TABLE "order".{table}
                DROP COLUMN accepted_carrier_origin_location_code,
                DROP COLUMN accepted_carrier_destination_location_code,
                DROP COLUMN accepted_carrier_weight_grams,
                DROP COLUMN accepted_carrier_declared_value,
                DROP COLUMN accepted_carrier_length_cm,
                DROP COLUMN accepted_carrier_width_cm,
                DROP COLUMN accepted_carrier_height_cm;
                """);

            migrationBuilder.Sql(
                $"""
                ALTER TABLE "order".{table}
                ADD CONSTRAINT ck_{table}_accepted_carrier_quote_complete CHECK (
                    (accepted_carrier_provider_code IS NULL
                        AND accepted_carrier_account_id IS NULL
                        AND accepted_carrier_configuration_version IS NULL
                        AND accepted_carrier_service_code IS NULL
                        AND accepted_carrier_quote_input_identity IS NULL
                        AND accepted_carrier_quote_currency IS NULL
                        AND accepted_carrier_final_amount IS NULL
                        AND accepted_carrier_transport_amount IS NULL
                        AND accepted_carrier_tax_amount IS NULL
                        AND accepted_carrier_packaging_component IS NULL
                        AND accepted_carrier_pickup_amount IS NULL
                        AND accepted_carrier_delivery_amount IS NULL
                        AND accepted_carrier_issued_at IS NULL
                        AND accepted_carrier_fresh_until IS NULL
                        AND accepted_carrier_provider_reference IS NULL)
                    OR
                    (accepted_carrier_provider_code IS NOT NULL
                        AND accepted_carrier_account_id IS NOT NULL
                        AND accepted_carrier_configuration_version IS NOT NULL
                        AND accepted_carrier_service_code IS NOT NULL
                        AND accepted_carrier_quote_input_identity IS NOT NULL
                        AND accepted_carrier_quote_currency IS NOT NULL
                        AND accepted_carrier_final_amount IS NOT NULL
                        AND accepted_carrier_transport_amount IS NOT NULL
                        AND accepted_carrier_tax_amount IS NOT NULL
                        AND accepted_carrier_packaging_component IS NOT NULL
                        AND accepted_carrier_pickup_amount IS NOT NULL
                        AND accepted_carrier_delivery_amount IS NOT NULL
                        AND accepted_carrier_issued_at IS NOT NULL
                        AND accepted_carrier_fresh_until IS NOT NULL)
                );
                """);
        }
    }
}
