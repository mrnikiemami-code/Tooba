using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Offer.Application.Offers.Ports;
using Xunit;

namespace Tooba.Offer.Tests.Application;

public sealed class OfferVersionGuardTests
{
    private static readonly DateTimeOffset Version = new(2026, 11, 26, 10, 30, 15, 123, TimeSpan.Zero);

    [Fact]
    public void Missing_version_is_backward_compatible()
    {
        Assert.Null(OfferVersionGuard.Check(Version, null));
        Assert.Equal(FoundationErrorCodes.ConcurrencyConflict, OfferVersionGuard.Check(Version, " ")?.Code);
    }

    [Fact]
    public void Exact_round_trip_version_is_current()
    {
        Assert.Null(OfferVersionGuard.Check(Version, Version.ToString("O")));
    }

    [Fact]
    public void Malformed_or_stale_version_fails_with_the_shared_conflict_code()
    {
        var malformed = OfferVersionGuard.Check(Version, "not-a-date");
        Assert.NotNull(malformed);
        Assert.Equal(FoundationErrorCodes.ConcurrencyConflict, malformed.Code);

        var stale = OfferVersionGuard.Check(Version, Version.AddMilliseconds(-1).ToString("O"));
        Assert.NotNull(stale);
        Assert.Equal(FoundationErrorCodes.ConcurrencyConflict, stale.Code);
    }
}
