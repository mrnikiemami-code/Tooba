using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Carriers;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Infrastructure.Carriers;
using Xunit;

namespace Tooba.Shipping.Tests.Carriers;

public sealed class ShippingCarrierDispatchIntentTests
{
    private static readonly Guid Checkout = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid Group = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb");
    private static readonly Guid Line = Guid.Parse("cccccccc-cccc-4ccc-8ccc-cccccccccccc");

    [Fact]
    public void Distinct_occurrences_with_identical_payload_produce_distinct_operation_keys()
    {
        var owner = ShippingDraftOwner.ForStore("store-alpha");
        var lines = new[] { new ShippingCarrierShipmentLineQuantity(Line, 1m) };
        var package = new ShippingCarrierShipmentPackageInput(500m, 0m, 10m, 10m, 10m, 0m);
        var a = ShippingCarrierDispatchIntent.MintOccurrenceId();
        var b = ShippingCarrierDispatchIntent.MintOccurrenceId();
        Assert.NotEqual(a, b);

        var (keyA, fpA) = ShippingCarrierDispatchIntent.Derive(a, owner, Checkout, Group, lines, package);
        var (keyB, fpB) = ShippingCarrierDispatchIntent.Derive(b, owner, Checkout, Group, lines, package);

        Assert.NotEqual(keyA, keyB);
        Assert.Equal(fpA, fpB);
        Assert.StartsWith($"c:{Checkout:N}:{Group:N}:", keyA);
        Assert.Contains(a.ToString("N"), keyA, StringComparison.OrdinalIgnoreCase);
        Assert.True(keyA.Length <= 128);
    }

    [Fact]
    public void Same_occurrence_same_payload_is_stable_replay_fingerprint()
    {
        var owner = ShippingDraftOwner.ForStore("store-alpha");
        var occurrence = UuidV7.New();
        var lines = new[] { new ShippingCarrierShipmentLineQuantity(Line, 1m) };
        var package = new ShippingCarrierShipmentPackageInput(500m, 0m, 10m, 10m, 10m, 0m);

        var (key1, fp1) = ShippingCarrierDispatchIntent.Derive(occurrence, owner, Checkout, Group, lines, package);
        var (key2, fp2) = ShippingCarrierDispatchIntent.Derive(occurrence, owner, Checkout, Group, lines, package);

        Assert.Equal(key1, key2);
        Assert.Equal(fp1, fp2);
        Assert.True(ShippingCarrierDispatchIntent.PayloadMatches(fp1, owner, Checkout, Group, lines, package));
    }

    [Fact]
    public void Same_occurrence_different_payload_fingerprint_diverges()
    {
        var owner = ShippingDraftOwner.ForStore("store-alpha");
        var occurrence = UuidV7.New();
        var lines = new[] { new ShippingCarrierShipmentLineQuantity(Line, 1m) };
        var light = new ShippingCarrierShipmentPackageInput(500m, 0m, 10m, 10m, 10m, 0m);
        var heavy = new ShippingCarrierShipmentPackageInput(900m, 0m, 10m, 10m, 10m, 0m);

        var (keyLight, fpLight) = ShippingCarrierDispatchIntent.Derive(occurrence, owner, Checkout, Group, lines, light);
        var (keyHeavy, fpHeavy) = ShippingCarrierDispatchIntent.Derive(occurrence, owner, Checkout, Group, lines, heavy);

        Assert.Equal(keyLight, keyHeavy);
        Assert.NotEqual(fpLight, fpHeavy);
        Assert.False(ShippingCarrierDispatchIntent.PayloadMatches(fpLight, owner, Checkout, Group, lines, heavy));
    }
}
