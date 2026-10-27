using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// Both relational evidence classes share ONE task-owned database and their fixtures reset whole tables, so
/// they must never run in parallel with each other (xUnit would otherwise interleave the resets).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class OrderEvidenceRelationalCollection
{
    /// <summary>Name shared by every class that uses <see cref="OrderEvidenceRelationalTestTarget"/>.</summary>
    public const string Name = "order-evidence-relational";
}
