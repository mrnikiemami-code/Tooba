using Xunit;

namespace Tooba.Fulfillment.Tests.Behavior;

/// <summary>
/// TB-SHIPPING-ADMIN-SHIPPING-TYPE-CATALOG-196-R7 — the R6 full-schema gate must stop on an unproved or failed
/// prerequisite instead of continuing into model projection or latest Up.
/// </summary>
public sealed class TypeCatalogFullSchemaGateTests
{
    [Fact]
    public void Unproved_snapshot_equivalence_stops_before_projection_or_up()
    {
        var stop = TypeCatalogPostgresSchemaFixture.EvaluateFullSchemaPrerequisite(
            snapshotEqualsCurrentModel: false,
            modelDifferences: ["CreateTableOperation", "AlterColumnOperation"],
            substantiveLegacyDifferences: []);

        Assert.NotNull(stop);
        Assert.Contains("UNPROVEN", stop);
        Assert.Contains("2 model differences", stop);
    }

    [Fact]
    public void Substantive_legacy_discrepancies_stop_before_latest_up()
    {
        var stop = TypeCatalogPostgresSchemaFixture.EvaluateFullSchemaPrerequisite(
            snapshotEqualsCurrentModel: true,
            modelDifferences: [],
            substantiveLegacyDifferences: ["missing-in-actual: index fulfillment.outbox_messages"]);

        Assert.NotNull(stop);
        Assert.Contains("substantive", stop);
        Assert.Contains("outbox_messages", stop);
    }

    [Fact]
    public void Green_prerequisites_do_not_stop()
    {
        Assert.Null(TypeCatalogPostgresSchemaFixture.EvaluateFullSchemaPrerequisite(
            snapshotEqualsCurrentModel: true,
            modelDifferences: [],
            substantiveLegacyDifferences: []));
    }
}
