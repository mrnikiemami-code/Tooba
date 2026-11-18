using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tooba.Returns.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// TB-279 repair: the audited decision/correction migration was authored with a quoted PascalCase
    /// <c>"ApprovedAmount"</c> column while this module's model uses the global snake_case convention, so every owning
    /// read of the frozen approved amount resolved <c>approved_amount</c> and the case detail answered a generic 500.
    /// This migration renames exactly that column to the name the model already expects. It is idempotent: a database
    /// that already has <c>approved_amount</c> is untouched, and no row is rewritten, backfilled or dropped.
    /// </summary>
    public partial class NormalizeApprovedAmountColumn : Migration
    {
        private const string RenameUp = """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'returns' AND table_name = 'return_requests' AND column_name = 'ApprovedAmount')
                AND NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'returns' AND table_name = 'return_requests' AND column_name = 'approved_amount')
                THEN
                    ALTER TABLE returns.return_requests RENAME COLUMN "ApprovedAmount" TO approved_amount;
                END IF;
            END $$;
            """;

        private const string RenameDown = """
            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'returns' AND table_name = 'return_requests' AND column_name = 'approved_amount')
                AND NOT EXISTS (
                    SELECT 1 FROM information_schema.columns
                    WHERE table_schema = 'returns' AND table_name = 'return_requests' AND column_name = 'ApprovedAmount')
                THEN
                    ALTER TABLE returns.return_requests RENAME COLUMN approved_amount TO "ApprovedAmount";
                END IF;
            END $$;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(RenameUp);

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.Sql(RenameDown);
    }
}
