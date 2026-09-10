using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Host.Fulfillment;
using Tooba.Host.Storefront;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// TB-P09-T021-R1: ترجیح رهگیری بسته تجمیعی + مرز دسترسی مشتری/مهمان.
/// </summary>
public sealed class CustomerFulfillmentTrackingTests
{
    [Fact]
    public void Preferred_package_includes_delivered_and_excludes_cancelled()
    {
        var checkoutId = Guid.NewGuid();
        var cancelled = Package(checkoutId, "MP-OLD", ConsolidatedPackageStatus.Cancelled, "OLD-TRK", daysAgo: 2);
        var delivered = Package(checkoutId, "MP-NEW", ConsolidatedPackageStatus.Delivered, "CENTRAL-DELIVERED", daysAgo: 0);
        var preferred = FulfillmentPanelComposer.SelectPreferredCustomerPackage([cancelled, delivered]);
        Assert.NotNull(preferred);
        Assert.Equal("MP-NEW", preferred!.PackageNumber);
        Assert.Equal("CENTRAL-DELIVERED", preferred.TrackingReference);
        Assert.Equal(ConsolidatedPackageStatus.Delivered, preferred.Status);
    }

    [Fact]
    public void Preferred_package_rebuild_prefers_new_active_over_cancelled()
    {
        var checkoutId = Guid.NewGuid();
        var cancelled = Package(checkoutId, "MP-1", ConsolidatedPackageStatus.Cancelled, "TRK-1", daysAgo: 1);
        var created = Package(checkoutId, "MP-2", ConsolidatedPackageStatus.Created, "TRK-2", daysAgo: 0);
        var preferred = FulfillmentPanelComposer.SelectPreferredCustomerPackage([cancelled, created]);
        Assert.NotNull(preferred);
        Assert.Equal("MP-2", preferred!.PackageNumber);
    }

    [Fact]
    public void Preferred_package_null_when_only_cancelled_or_empty()
    {
        var checkoutId = Guid.NewGuid();
        Assert.Null(FulfillmentPanelComposer.SelectPreferredCustomerPackage([]));
        Assert.Null(FulfillmentPanelComposer.SelectPreferredCustomerPackage(
        [
            Package(checkoutId, "MP-X", ConsolidatedPackageStatus.Cancelled, "TRK-X", daysAgo: 0),
        ]));
    }

    [Fact]
    public void Customer_fulfillment_endpoint_reuses_guest_actor_and_guest_secret_proof()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "backend",
            "Host",
            "Tooba.Host",
            "Fulfillment",
            "FulfillmentEndpoints.cs"));
        Assert.Contains("StorefrontGuestActorId", source, StringComparison.Ordinal);
        Assert.Contains("X-Tooba-Guest-Secret", source, StringComparison.Ordinal);
        Assert.Contains("ICartQueryGateway", source, StringComparison.Ordinal);
        Assert.Contains("SelectPreferredCustomerPackage", source, StringComparison.Ordinal);
        Assert.Contains("preferredCustomerPackageStatus", source, StringComparison.Ordinal);
        Assert.Contains("CartAccess", source, StringComparison.Ordinal);
        Assert.Contains("ownedByActor = false", source, StringComparison.Ordinal);
        Assert.True(StorefrontCheckoutComposer.StorefrontGuestActorId != Guid.Empty);
    }

    private static ConsolidatedPackageSnapshot Package(
        Guid checkoutId,
        string number,
        ConsolidatedPackageStatus status,
        string? tracking,
        int daysAgo)
    {
        var at = DateTimeOffset.UtcNow.AddDays(-daysAgo);
        return new ConsolidatedPackageSnapshot(
            Guid.NewGuid(),
            number,
            checkoutId,
            status,
            "post",
            "پست",
            tracking,
            null,
            Guid.NewGuid(),
            at,
            at,
            status is ConsolidatedPackageStatus.Dispatched or ConsolidatedPackageStatus.Delivered ? at : null,
            status == ConsolidatedPackageStatus.Delivered ? at : null,
            status == ConsolidatedPackageStatus.Cancelled ? at : null,
            []);
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
