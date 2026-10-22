using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Infrastructure.Rates;
using Xunit;

namespace Tooba.Shipping.Tests.Rates;

/// <summary>
/// TB-SHIPPING-NEW-METHOD-ACTIVATION-170 — کلید نرخ مالک برای روش تازهٔ canonical.
/// تمرکز: هویت واقعی روش (سرویس + گزینهٔ پیوند) پذیرفته شود، کد ساختگی رد شود و دروازهٔ خاموش
/// هیچ امتیازی ندهد.
/// </summary>
public sealed class ShippingCanonicalMethodRateKeyTests
{
    private static readonly Guid PostServiceId = Guid.Parse("aaaaaaaa-aaaa-7aaa-8aaa-aaaaaaaaaaaa");
    private static readonly Guid StandardOptionId = Guid.Parse("dddddddd-dddd-7ddd-8ddd-dddddddddddd");

    private sealed class CatalogFake : IShippingCatalogReader
    {
        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingCatalogServiceSnapshot>>([Service]);

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken) =>
            Task.FromResult<ShippingCatalogServiceSnapshot?>(serviceId == PostServiceId ? Service : null);

        private static readonly ShippingCatalogServiceSnapshot Service = new(
            PostServiceId,
            "post",
            "post",
            "post",
            "blue",
            true,
            10,
            [],
            [new ShippingCatalogOptionSnapshot(StandardOptionId, PostServiceId, "standard", true, 20, [])]);
    }

    private sealed class CanonicalFake(params string[] bindings) : IShippingOneLevelCanonicalMethodReader
    {
        private readonly HashSet<string> _bindings = new(bindings, StringComparer.OrdinalIgnoreCase);

        public Task<IReadOnlyList<ShippingOneLevelCanonicalMethodSnapshot>> ListActiveUnboundAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ShippingOneLevelCanonicalMethodSnapshot>>(
                _bindings.Select(b => new ShippingOneLevelCanonicalMethodSnapshot(
                    $"post:{b}", "post", b, "post", 10)).ToArray());

        public Task<bool> IsCanonicalMethodBindingAsync(
            string serviceCode,
            string? optionCode,
            CancellationToken cancellationToken) =>
            Task.FromResult(
                string.Equals(serviceCode, "post", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(optionCode)
                && _bindings.Contains(optionCode));
    }

    [Fact]
    public async Task A_legacy_catalog_option_is_still_accepted_unchanged()
    {
        var validator = new FulfillmentShippingMethodOptionValidator(new CatalogFake(), new ModuleCallTracer(), new CanonicalFake());

        Assert.True((await validator.ValidateAsync("post", "standard", CancellationToken.None)).IsSuccess);
        Assert.True((await validator.ValidateAsync("post", null, CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task A_new_canonical_method_binding_becomes_a_valid_rate_key_without_a_legacy_option_row()
    {
        var validator = new FulfillmentShippingMethodOptionValidator(new CatalogFake(), new ModuleCallTracer(), new CanonicalFake("economy"));

        Assert.True((await validator.ValidateAsync("post", "economy", CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task An_invented_option_or_a_missing_seam_is_refused()
    {
        var validator = new FulfillmentShippingMethodOptionValidator(new CatalogFake(), new ModuleCallTracer(), new CanonicalFake("economy"));
        var refused = await validator.ValidateAsync("post", "invented", CancellationToken.None);
        Assert.True(refused.IsFailure);
        Assert.Equal(ShippingErrorCodes.ServiceOptionUnknown, refused.Errors.Single().Code);

        var withoutSeam = new FulfillmentShippingMethodOptionValidator(new CatalogFake(), new ModuleCallTracer());
        var rejected = await withoutSeam.ValidateAsync("post", "economy", CancellationToken.None);
        Assert.True(rejected.IsFailure);
        Assert.Equal(ShippingErrorCodes.ServiceOptionUnknown, rejected.Errors.Single().Code);
    }
}
