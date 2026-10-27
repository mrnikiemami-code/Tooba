using Tooba.BuildingBlocks;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Application.Storefront.Shipping.CapturedGroupProjection;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Order.Domain.Checkout;
using Tooba.Shipping.Contracts.Rates;
using Xunit;

namespace Tooba.Order.Tests.Checkout;

/// <summary>
/// TB-235: ماتریس <b>یک</b> سیاست مالک ارسال — همان سیاستی که خواندن، نوشتن و پذیرش نهایی به کار می‌برند.
/// </summary>
/// <remarks>
/// <para>
/// این آزمون فقط قاعدهٔ <b>استنتاج</b> مالک از زمینهٔ مورداعتماد + انتساب واقعی ۲۲۸ را قفل می‌کند؛ هیچ
/// مالک/tenant/فروشنده‌ای از بدنه ساخته نمی‌شود و هیچ مالکی به دیگری بازتفسیر نمی‌شود. اثبات عملکردی
/// واقعی (Marketplace دو مالک در یک سبد، Store دو فروشنده) در آزمون‌های relational همین پوشه است.
/// </para>
/// </remarks>
public sealed class StorefrontCapturedOwnerPolicyTests
{
    private static readonly Guid TenantA = Guid.Parse("0199b0a1-0000-7000-8000-00000000a001");
    private static readonly Guid SellerA = Guid.Parse("0199b0a1-0000-7000-8000-00000000a002");
    private static readonly Guid SellerB = Guid.Parse("0199b0a1-0000-7000-8000-00000000a003");

    [Fact]
    public void SingleStore_platform_is_the_active_tenant_store_owner()
    {
        var resolved = StorefrontCapturedOwnerPolicy.Resolve(
            ToobaEdition.SingleStore,
            Tenant("store-alpha", TenantStatus.Active),
            OfferFulfillmentResponsibility.Platform,
            SellerA);

        Assert.Equal(StorefrontCapturedOwnerFailure.None, resolved.Failure);
        var owner = Assert.IsType<StorefrontCapturedOwner>(resolved.Owner);
        Assert.Equal(ShippingCapturedQuoteOwnerKind.Store, owner.Kind);
        Assert.Equal("store-alpha", owner.TenantId);
        Assert.Null(owner.SellerPartyId);
        // مالک Store هرگز فروشندهٔ خط را به‌عنوان کلید مالک نمی‌نویسد.
        Assert.Null(owner.ToDomainOwner().SellerPartyId);
    }

    [Fact]
    public void SingleStore_seller_assignment_is_unsupported_and_never_reinterpreted_as_the_store()
    {
        var resolved = StorefrontCapturedOwnerPolicy.Resolve(
            ToobaEdition.SingleStore,
            Tenant("store-alpha", TenantStatus.Active),
            OfferFulfillmentResponsibility.Seller,
            SellerA);

        Assert.Null(resolved.Owner);
        Assert.Equal(StorefrontCapturedOwnerFailure.SellerUnsupportedInSingleStore, resolved.Failure);
    }

    [Theory]
    [InlineData(null, TenantStatus.Active)]
    [InlineData("store-alpha", TenantStatus.Disabled)]
    [InlineData("   ", TenantStatus.Active)]
    public void SingleStore_without_an_active_exact_tenant_has_no_owner(string? tenantId, TenantStatus status)
    {
        var resolved = StorefrontCapturedOwnerPolicy.Resolve(
            ToobaEdition.SingleStore,
            tenantId is null ? null : Tenant(tenantId, status),
            OfferFulfillmentResponsibility.Platform,
            SellerA);

        Assert.Null(resolved.Owner);
        Assert.Equal(StorefrontCapturedOwnerFailure.OwnerUnavailable, resolved.Failure);
    }

    [Fact]
    public void Marketplace_platform_is_the_marketplace_owner_without_tenant_or_seller_key()
    {
        var resolved = StorefrontCapturedOwnerPolicy.Resolve(
            ToobaEdition.Marketplace,
            tenant: null,
            OfferFulfillmentResponsibility.Platform,
            SellerB);

        Assert.Equal(StorefrontCapturedOwnerFailure.None, resolved.Failure);
        var owner = Assert.IsType<StorefrontCapturedOwner>(resolved.Owner);
        Assert.Equal(ShippingCapturedQuoteOwnerKind.Marketplace, owner.Kind);
        Assert.Null(owner.TenantId);
        Assert.Null(owner.SellerPartyId);
        var domain = owner.ToDomainOwner();
        Assert.Equal(ShippingGroupOwnerKind.Marketplace, domain.Kind);
        Assert.Null(domain.TenantKey);
        Assert.Null(domain.SellerPartyId);
    }

    [Fact]
    public void Marketplace_residual_tenant_context_is_invalid_instead_of_being_ignored()
    {
        var resolved = StorefrontCapturedOwnerPolicy.Resolve(
            ToobaEdition.Marketplace,
            Tenant("store-alpha", TenantStatus.Active),
            OfferFulfillmentResponsibility.Platform,
            SellerB);

        Assert.Null(resolved.Owner);
        Assert.Equal(StorefrontCapturedOwnerFailure.OwnerUnavailable, resolved.Failure);
    }

    [Fact]
    public void Marketplace_seller_fulfillment_is_keyed_by_the_real_selling_party_of_that_group()
    {
        var resolved = StorefrontCapturedOwnerPolicy.Resolve(
            ToobaEdition.Marketplace,
            tenant: null,
            OfferFulfillmentResponsibility.Seller,
            SellerB);

        Assert.Equal(StorefrontCapturedOwnerFailure.None, resolved.Failure);
        var owner = Assert.IsType<StorefrontCapturedOwner>(resolved.Owner);
        Assert.Equal(ShippingCapturedQuoteOwnerKind.Seller, owner.Kind);
        Assert.Null(owner.TenantId);
        Assert.Equal(SellerB, owner.SellerPartyId);
        // دو فروشندهٔ واقعی دو مالک جدا می‌سازند؛ هیچ ادغامی روی tenant/برچسب نیست.
        var other = StorefrontCapturedOwnerPolicy.Resolve(
            ToobaEdition.Marketplace,
            tenant: null,
            OfferFulfillmentResponsibility.Seller,
            SellerA);
        Assert.NotEqual(owner, other.Owner);
    }

    [Fact]
    public void Marketplace_seller_without_a_real_selling_party_has_no_owner()
    {
        var resolved = StorefrontCapturedOwnerPolicy.Resolve(
            ToobaEdition.Marketplace,
            tenant: null,
            OfferFulfillmentResponsibility.Seller,
            Guid.Empty);

        Assert.Null(resolved.Owner);
        Assert.Equal(StorefrontCapturedOwnerFailure.OwnerUnavailable, resolved.Failure);
    }

    [Fact]
    public void Unknown_edition_responsibility_or_enum_value_fails_before_any_owner_is_guessed()
    {
        var unknownEdition = StorefrontCapturedOwnerPolicy.Resolve(
            edition: null,
            Tenant("store-alpha", TenantStatus.Active),
            OfferFulfillmentResponsibility.Platform,
            SellerA);
        Assert.Equal(StorefrontCapturedOwnerFailure.OwnerUnavailable, unknownEdition.Failure);

        var missing = StorefrontCapturedOwnerPolicy.Resolve(
            ToobaEdition.Marketplace,
            tenant: null,
            responsibility: null,
            SellerA);
        Assert.Equal(StorefrontCapturedOwnerFailure.ResponsibilityMissing, missing.Failure);

        var undefined = StorefrontCapturedOwnerPolicy.Resolve(
            ToobaEdition.Marketplace,
            tenant: null,
            (OfferFulfillmentResponsibility)97,
            SellerA);
        Assert.Equal(StorefrontCapturedOwnerFailure.ResponsibilityInvalid, undefined.Failure);

        var domainUndefined = StorefrontCapturedOwnerPolicy.Resolve(
            ToobaEdition.SingleStore,
            Tenant("store-alpha", TenantStatus.Active),
            (OfferFulfillmentResponsibility)97,
            SellerA);
        Assert.Equal(StorefrontCapturedOwnerFailure.ResponsibilityInvalid, domainUndefined.Failure);
    }

    [Fact]
    public void Stored_owner_comparison_is_exact_and_never_approximate()
    {
        var store = new StorefrontCapturedOwner(ShippingCapturedQuoteOwnerKind.Store, "store-alpha", null);
        Assert.True(store.IsSameAs(ShippingGroupOwner.Store("store-alpha")));
        // tenant حساس به حروف است (Ordinal) — هیچ lowercasing/نرمال‌سازی حدسی پذیرفته نمی‌شود.
        Assert.False(store.IsSameAs(ShippingGroupOwner.Store("Store-Alpha")));
        // کارخانهٔ دامنه همان سیاست متعارف trim را دارد؛ پس tenant با فاصلهٔ اضافی همان مقدار است
        // (مقایسه همچنان Ordinal است، نه تقریبی).
        Assert.True(store.IsSameAs(ShippingGroupOwner.Store(" store-alpha ")));
        // مالک Store هرگز با مالک Marketplace/Seller هم‌معنا نیست.
        Assert.False(store.IsSameAs(ShippingGroupOwner.Marketplace()));
        Assert.False(store.IsSameAs(ShippingGroupOwner.Seller(SellerA)));

        var marketplace = new StorefrontCapturedOwner(ShippingCapturedQuoteOwnerKind.Marketplace, null, null);
        Assert.True(marketplace.IsSameAs(ShippingGroupOwner.Marketplace()));
        Assert.False(marketplace.IsSameAs(ShippingGroupOwner.Store("store-alpha")));
        Assert.False(marketplace.IsSameAs(ShippingGroupOwner.Seller(SellerA)));

        var seller = new StorefrontCapturedOwner(ShippingCapturedQuoteOwnerKind.Seller, null, SellerA);
        Assert.True(seller.IsSameAs(ShippingGroupOwner.Seller(SellerA)));
        Assert.False(seller.IsSameAs(ShippingGroupOwner.Seller(SellerB)));
        Assert.False(seller.IsSameAs(ShippingGroupOwner.Marketplace()));
        Assert.False(seller.IsSameAs(null));
    }

    [Fact]
    public void Method_identity_is_owner_plus_binding_so_two_owners_never_share_one_label_entry()
    {
        var marketplace = StorefrontCapturedPublicationBinding.For(
            ShippingCapturedQuoteOwnerKind.Marketplace, null, null, "post", "standard");
        var sellerA = StorefrontCapturedPublicationBinding.For(
            ShippingCapturedQuoteOwnerKind.Seller, null, SellerA, "post", "standard");
        var sellerAOtherCase = StorefrontCapturedPublicationBinding.For(
            ShippingCapturedQuoteOwnerKind.Seller, null, SellerA, " POST ", "STANDARD");
        var store = StorefrontCapturedPublicationBinding.For(
            ShippingCapturedQuoteOwnerKind.Store, "store-alpha", null, "post", "standard");

        // همان سرویس/گزینه با مالک دیگر = هویت انتشار دیگر (برچسب DB خودش).
        Assert.NotEqual(marketplace, sellerA);
        Assert.NotEqual(sellerA, store);
        Assert.NotEqual(marketplace, store);
        Assert.Equal(3, new[] { marketplace, sellerA, sellerAOtherCase, store, marketplace }.Distinct().Count());
        // همان مالک + همان binding با فاصله/حروف متفاوت = همان هویت.
        Assert.Equal(sellerA, sellerAOtherCase);
        Assert.Equal(sellerA.GetHashCode(), sellerAOtherCase.GetHashCode());
    }

    private static TenantContext Tenant(string tenantId, TenantStatus status) =>
        new(
            new TenantId(tenantId),
            status,
            new ConnectionReference("test"),
            null,
            null,
            null,
            "store.local",
            null);
}
