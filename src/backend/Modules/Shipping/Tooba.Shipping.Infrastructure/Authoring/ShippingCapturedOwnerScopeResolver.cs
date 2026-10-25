using Tooba.BuildingBlocks;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Shipping.Infrastructure.Authoring;

/// <summary>
/// نگاشت <b>واحد</b> هویت مالک Contracts به scope داخلی Shipping، با سنجش انطباق با زمینهٔ trusted
/// نصب/تجارت جاری.
/// </summary>
/// <remarks>
/// <para>
/// این helper هیچ قاعدهٔ تجاری اضافه نمی‌کند: فقط می‌سنجد که گونهٔ مالک درخواستی با حالت نصب واقعی و
/// tenant فعال همین درخواست می‌خواند. زمینهٔ ناقص/ناسازگار همیشه <c>null</c> است و هیچ مقدار پیش‌فرضی
/// ساخته نمی‌شود.
/// </para>
/// <para>
/// مشترک بین خواندن قیمت capture‌شده (TB-216) و خواندن انتخاب capture (TB-219) تا دو validator موازی
/// وجود نداشته باشد. هویت مالک <b>داده</b> است، نه مجوز actor؛ هیچ فروشنده‌ای به‌صورت پیش‌فرض رد نمی‌شود.
/// </para>
/// </remarks>
internal static class ShippingCapturedOwnerScopeResolver
{
    /// <summary>گونهٔ مالک درخواستی را با زمینهٔ جاری می‌سنجد.</summary>
    /// <param name="kind">گونهٔ مالک درخواستی.</param>
    /// <param name="tenantId">tenant درخواستی یا تهی.</param>
    /// <param name="sellerPartyId">فروشندهٔ درخواستی یا تهی.</param>
    /// <param name="edition">حالت نصب جاری.</param>
    /// <param name="tenant">زمینهٔ تجارت جاری.</param>
    internal static ShippingAuthoringOwnerScope? Resolve(
        ShippingCapturedQuoteOwnerKind kind,
        string? tenantId,
        Guid? sellerPartyId,
        ICurrentEdition edition,
        ICurrentTenant tenant)
        => edition.Current?.Edition switch
        {
            ToobaEdition.SingleStore => ResolveStore(kind, tenantId, sellerPartyId, tenant),
            ToobaEdition.Marketplace => ResolveMarketplace(kind, tenantId, sellerPartyId, tenant),
            _ => null,
        };

    private static ShippingAuthoringOwnerScope? ResolveStore(
        ShippingCapturedQuoteOwnerKind kind,
        string? tenantId,
        Guid? sellerPartyId,
        ICurrentTenant tenant)
    {
        var current = tenant.Current;
        var currentTenantId = current?.TenantId.Value;

        // In SingleStore a residual/inactive/missing tenant context is unusable; nothing is defaulted.
        if (current is null
            || current.Status != TenantStatus.Active
            || string.IsNullOrWhiteSpace(currentTenantId)
            || kind != ShippingCapturedQuoteOwnerKind.Store
            || sellerPartyId is not null
            || !string.Equals(tenantId?.Trim(), currentTenantId.Trim(), StringComparison.Ordinal))
        {
            return null;
        }

        return ShippingAuthoringOwnerScope.ForStore(currentTenantId);
    }

    private static ShippingAuthoringOwnerScope? ResolveMarketplace(
        ShippingCapturedQuoteOwnerKind kind,
        string? tenantId,
        Guid? sellerPartyId,
        ICurrentTenant tenant)
    {
        // Marketplace has no store tenant at all: a leftover tenant is an invalid context, not a guess.
        if (tenant.Current is not null || !string.IsNullOrWhiteSpace(tenantId))
        {
            return null;
        }

        return kind switch
        {
            ShippingCapturedQuoteOwnerKind.Marketplace when sellerPartyId is null =>
                ShippingAuthoringOwnerScope.ForMarketplace(),
            ShippingCapturedQuoteOwnerKind.Seller when sellerPartyId is { } sellerId && sellerId != Guid.Empty =>
                ShippingAuthoringOwnerScope.ForSeller(sellerId),
            _ => null,
        };
    }
}
