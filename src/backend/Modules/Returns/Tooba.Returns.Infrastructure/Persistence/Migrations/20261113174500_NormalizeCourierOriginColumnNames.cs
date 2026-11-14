using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Returns.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-265 repair: the first TB-265 Returns migration was authored with PascalCase column names while the model
    /// maps snake_case, so on a database that already ran it the durability columns lived under quoted PascalCase
    /// names and the owning queries could not read them. This migration renames exactly those columns to the names
    /// the model already expects. It is idempotent: a fresh database that ran the corrected migration has the
    /// snake_case columns already, so every guarded rename is a no-op. No data is rewritten, backfilled or dropped.
    /// </summary>
    public partial class NormalizeCourierOriginColumnNames : Migration
    {
        private const string RenameUp = """
            DO $$
            DECLARE
                pair text[];
            BEGIN
                FOREACH pair SLICE 1 IN ARRAY ARRAY[
                    ARRAY['return_requests', 'Origin', 'origin'],
                    ARRAY['return_requests', 'SourceFulfillmentId', 'source_fulfillment_id'],
                    ARRAY['return_requests', 'SourceShipmentId', 'source_shipment_id'],
                    ARRAY['return_requests', 'SourceReceiptId', 'source_receipt_id'],
                    ARRAY['return_requests', 'SourceOccurrenceKey', 'source_occurrence_key'],
                    ARRAY['return_requests', 'SourceClaimId', 'source_claim_id'],
                    ARRAY['return_requests', 'ObligationClosedAt', 'obligation_closed_at'],
                    ARRAY['return_requests', 'StockSettledAt', 'stock_settled_at'],
                    ARRAY['return_requests', 'ClosurePhase', 'closure_phase'],
                    ARRAY['return_requests', 'ReleasedHeldQuantity', 'released_held_quantity'],
                    ARRAY['return_items', 'Condition', 'condition'],
                    ARRAY['return_items', 'Disposition', 'disposition'],
                    ARRAY['return_items', 'SourceShipmentId', 'source_shipment_id'],
                    ARRAY['return_items', 'SourceReceiptId', 'source_receipt_id']
                ]
                LOOP
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'returns' AND table_name = pair[1] AND column_name = pair[2])
                    AND NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'returns' AND table_name = pair[1] AND column_name = pair[3])
                    THEN
                        EXECUTE format('ALTER TABLE returns.%I RENAME COLUMN %I TO %I', pair[1], pair[2], pair[3]);
                    END IF;
                END LOOP;
            END $$;
            """;

        private const string RenameDown = """
            DO $$
            DECLARE
                pair text[];
            BEGIN
                FOREACH pair SLICE 1 IN ARRAY ARRAY[
                    ARRAY['return_requests', 'origin', 'Origin'],
                    ARRAY['return_requests', 'source_fulfillment_id', 'SourceFulfillmentId'],
                    ARRAY['return_requests', 'source_shipment_id', 'SourceShipmentId'],
                    ARRAY['return_requests', 'source_receipt_id', 'SourceReceiptId'],
                    ARRAY['return_requests', 'source_occurrence_key', 'SourceOccurrenceKey'],
                    ARRAY['return_requests', 'source_claim_id', 'SourceClaimId'],
                    ARRAY['return_requests', 'obligation_closed_at', 'ObligationClosedAt'],
                    ARRAY['return_requests', 'stock_settled_at', 'StockSettledAt'],
                    ARRAY['return_requests', 'closure_phase', 'ClosurePhase'],
                    ARRAY['return_requests', 'released_held_quantity', 'ReleasedHeldQuantity'],
                    ARRAY['return_items', 'condition', 'Condition'],
                    ARRAY['return_items', 'disposition', 'Disposition'],
                    ARRAY['return_items', 'source_shipment_id', 'SourceShipmentId'],
                    ARRAY['return_items', 'source_receipt_id', 'SourceReceiptId']
                ]
                LOOP
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'returns' AND table_name = pair[1] AND column_name = pair[2])
                    AND NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'returns' AND table_name = pair[1] AND column_name = pair[3])
                    THEN
                        EXECUTE format('ALTER TABLE returns.%I RENAME COLUMN %I TO %I', pair[1], pair[2], pair[3]);
                    END IF;
                END LOOP;
            END $$;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(RenameUp);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(RenameDown);
    }
}
