using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Order.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Order.Infrastructure.Persistence.Migrations;

/// <summary>TB-248-R1: additive carrier quote evidence on accepted shipping groups. Manual/Free stay null.</summary>
[DbContext(typeof(OrderDbContext))]
[Migration("20261102180000_AddAcceptedCarrierQuoteEvidence")]
public partial class AddAcceptedCarrierQuoteEvidence : Migration
{
    private static readonly string[] Tables = ["cart_shipping_groups", "order_shipping_groups"];

    private static readonly string[] Columns =
    [
        "accepted_carrier_provider_code character varying(64) NULL",
        "accepted_carrier_account_id uuid NULL",
        "accepted_carrier_configuration_version integer NULL",
        "accepted_carrier_service_code character varying(64) NULL",
        "accepted_carrier_quote_input_identity character varying(128) NULL",
        "accepted_carrier_quote_currency character varying(3) NULL",
        "accepted_carrier_final_amount numeric(18,2) NULL",
        "accepted_carrier_transport_amount numeric(18,2) NULL",
        "accepted_carrier_tax_amount numeric(18,2) NULL",
        "accepted_carrier_packaging_component numeric(18,2) NULL",
        "accepted_carrier_pickup_amount numeric(18,2) NULL",
        "accepted_carrier_delivery_amount numeric(18,2) NULL",
        "accepted_carrier_issued_at timestamp with time zone NULL",
        "accepted_carrier_fresh_until timestamp with time zone NULL",
        "accepted_carrier_provider_reference character varying(128) NULL",
    ];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in Tables)
        {
            foreach (var column in Columns)
            {
                migrationBuilder.Sql($"ALTER TABLE \"order\".{table} ADD COLUMN {column};");
            }

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

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var table in Tables)
        {
            migrationBuilder.Sql($"ALTER TABLE \"order\".{table} DROP CONSTRAINT ck_{table}_accepted_carrier_quote_complete;");
            migrationBuilder.Sql(
                $"""
                ALTER TABLE "order".{table}
                DROP COLUMN accepted_carrier_provider_code,
                DROP COLUMN accepted_carrier_account_id,
                DROP COLUMN accepted_carrier_configuration_version,
                DROP COLUMN accepted_carrier_service_code,
                DROP COLUMN accepted_carrier_quote_input_identity,
                DROP COLUMN accepted_carrier_quote_currency,
                DROP COLUMN accepted_carrier_final_amount,
                DROP COLUMN accepted_carrier_transport_amount,
                DROP COLUMN accepted_carrier_tax_amount,
                DROP COLUMN accepted_carrier_packaging_component,
                DROP COLUMN accepted_carrier_pickup_amount,
                DROP COLUMN accepted_carrier_delivery_amount,
                DROP COLUMN accepted_carrier_issued_at,
                DROP COLUMN accepted_carrier_fresh_until,
                DROP COLUMN accepted_carrier_provider_reference;
                """);
        }
    }
}
