using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Geography.Contracts.Places;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Offer.Contracts.Ports;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Models;
using Tooba.Order.Application.Storefront.Services;
using Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Order.Tests.Checkout;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Storefront;

/// <summary>
/// TB-229-R1: دروازهٔ نوشتن پیش‌نمایش capture‌شده — از طریق <b>همان متدهای سرویس</b>.
/// </summary>
/// <remarks>
/// <para>
/// وابستگی‌های سرویس عمداً <c>null</c> هستند: اگر دروازه اولین دستور نباشد و سبد/پیش‌نویس/checkout لمس
/// شود، به‌جای امتناع پایدار یک <see cref="NullReferenceException"/> می‌گیریم. پس رد شدن با کد صریح،
/// خودش اثبات «پیش از هر خواندن/نوشتن» است.
/// </para>
/// <para>
/// خاموش‌بودن کلید همان رفتار قبلی است: همان فراخوانی دیگر کد «آماده‌نیست» نمی‌دهد و مسیر قدیمی
/// دست‌نخورده می‌ماند (اینجا به‌خاطر وابستگی تهی با خطای دیگری متوقف می‌شود، نه با امتناع پیش‌نمایش).
/// </para>
/// </remarks>
public sealed class StorefrontCapturedPreviewWriteGuardTests
{
    private static readonly Guid CartId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Save_selection_is_refused_on_the_preview_code_before_any_dependency()
    {
        var service = Service(enabled: true);

        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            service.SaveSelectionAsync(Selection(), null, CancellationToken.None));

        Assert.Equal(StorefrontCapturedPreviewReasons.PreviewNotReady, error.Code);
    }

    [Fact]
    public async Task Commit_is_refused_on_the_preview_code_before_any_dependency()
    {
        var service = Service(enabled: true);

        var error = await Assert.ThrowsAsync<StorefrontOrderException>(() =>
            service.CommitAsync(CartId, null, 7, "key", null, CancellationToken.None));

        Assert.Equal(StorefrontCapturedPreviewReasons.PreviewNotReady, error.Code);
    }

    [Fact]
    public async Task Disabled_option_keeps_the_existing_path_and_never_uses_the_preview_refusal()
    {
        var service = Service(enabled: false);

        var saveFailure = await Record.ExceptionAsync(() =>
            service.SaveSelectionAsync(Selection(), null, CancellationToken.None));
        var commitFailure = await Record.ExceptionAsync(() =>
            service.CommitAsync(CartId, null, 7, "key", null, CancellationToken.None));

        Assert.NotNull(saveFailure);
        Assert.NotNull(commitFailure);
        Assert.False(
            saveFailure is StorefrontOrderException saveError
            && saveError.Code == StorefrontCapturedPreviewReasons.PreviewNotReady);
        Assert.False(
            commitFailure is StorefrontOrderException commitError
            && commitError.Code == StorefrontCapturedPreviewReasons.PreviewNotReady);
    }

    private static StorefrontShippingService Service(bool enabled) =>
        new(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new ShippingMethodsOptions(),
            null!,
            null!,
            new MutableClock(DateTimeOffset.UtcNow),
            capturedPreview: new StorefrontCapturedPreviewPlanner(
                new NoCallAssignments(),
                new NoCallCatalog(),
                new NoCallTypes(),
                new NoCallSelected(),
                new StorefrontCapturedGroupProjectionComposer(
                    new FixedGoodsFactsReader(null),
                    new RecordingCapturedQuoteLookup(),
                    new MutableClock(DateTimeOffset.UtcNow)),
                new NoCallEdition(),
                new NoCallTenant(),
                Options.Create(new StorefrontCapturedPreviewOptions
                {
                    Enabled = enabled,
                    AvailableDateCount = enabled ? 7 : null,
                })));

    private static StorefrontShippingSelectionRequest Selection() =>
        new(
            CartId,
            7,
            "گیرنده",
            "09120000000",
            "تهران",
            "تهران",
            "خیابان",
            "1234567890",
            null,
            "post:standard",
            "2026-10-29",
            "morning",
            null);

    private sealed class NoCallAssignments : IOfferFulfillmentAssignmentLookup
    {
        public Task<IReadOnlyDictionary<Guid, OfferFulfillmentAssignmentSnapshot>> GetAssignmentsAsync(
            IReadOnlyCollection<Guid> offerIds,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("assignments must not be read");
    }

    private sealed class NoCallCatalog : IShippingCatalogReader
    {
        public Task<IReadOnlyList<ShippingCatalogServiceSnapshot>> ListAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("catalog must not be read");

        public Task<ShippingCatalogServiceSnapshot?> GetAsync(Guid serviceId, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("catalog must not be read");
    }

    private sealed class NoCallTypes : IShippingTypeReader
    {
        public Task<IReadOnlyList<ShippingTypeSnapshot>> ListActiveAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("types must not be read");

        public Task<IReadOnlyList<ShippingTypeSnapshot>> GetByIdsAsync(
            IReadOnlyCollection<Guid> shippingTypeIds,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("types must not be read");
    }

    private sealed class NoCallSelected : IShippingSelectedCapturedRevisionLookup
    {
        public Task<ShippingSelectedCapturedRevisionResult> ReadAsync(
            ShippingSelectedCapturedRevisionRequest request,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("selected capture must not be read");
    }

    private sealed class NoCallEdition : ICurrentEdition
    {
        public EditionContext? Current => throw new InvalidOperationException("edition must not be read");
    }

    private sealed class NoCallTenant : ICurrentTenant
    {
        public TenantContext? Current => throw new InvalidOperationException("tenant must not be read");
    }
}
