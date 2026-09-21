namespace Tooba.Order.Contracts.Fulfillment;

/// <summary>خط سفارش برای احراز مجوز category-scoped فروشنده.</summary>
public sealed record SellerOrderAuthLine(
    Guid OrderLineId,
    Guid CatalogVariantId,
    Guid? CategoryIdSnapshot);

/// <summary>سفارش فروشنده برای احراز order.handle (با fallback دسته در پیاده‌سازی Order).</summary>
public sealed record SellerOrderAuthSnapshot(
    Guid SellerOrderId,
    Guid SellerPartyId,
    IReadOnlyList<SellerOrderAuthLine> Lines);

/// <summary>
/// خواندن سفارش فروشنده برای احراز محدودهٔ Fulfillment بدون DbContext Order در Host/Fulfillment.
/// پیاده‌سازی Category snapshot + Catalog fallback را داخل Order.Infrastructure حل می‌کند.
/// </summary>
public interface ISellerOrderAuthReader
{
    /// <summary>سفارش فروشنده را با خطوط و دستهٔ مؤثر برمی‌گرداند؛ نبود → null.</summary>
    Task<SellerOrderAuthSnapshot?> GetForSellerAsync(
        Guid sellerOrderId,
        Guid sellerPartyId,
        CancellationToken cancellationToken);
}

/// <summary>مالکیت checkout مشتری برای لیست fulfillment (بدون OrderDbContext در Host endpoint).</summary>
public sealed record CustomerCheckoutOwnershipSnapshot(
    Guid CheckoutId,
    Guid PlacedByUserId,
    Guid CartId);

/// <summary>خواندن مالکیت checkout برای مسیر مشتری.</summary>
public interface ICustomerCheckoutOwnershipReader
{
    /// <summary>checkout را برای اثبات مالکیت برمی‌گرداند؛ نبود → null.</summary>
    Task<CustomerCheckoutOwnershipSnapshot?> GetAsync(Guid checkoutId, CancellationToken cancellationToken);
}

/// <summary>غنی‌سازی گرید Fulfillment/Returns از دادهٔ Order بدون foreign DbContext.</summary>
public interface IOrderGridEnrichmentReader
{
    /// <summary>شماره سفارش بر اساس SellerOrderId.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetOrderNumbersAsync(
        IReadOnlyList<Guid> sellerOrderIds,
        CancellationToken cancellationToken);

    /// <summary>جستجوی SellerOrderId بر اساس OrderNumber.</summary>
    Task<IReadOnlyList<Guid>> SearchSellerOrderIdsByOrderNumberAsync(
        string term,
        int take,
        CancellationToken cancellationToken);

    /// <summary>فیلتر متنی روی OrderNumber → SellerOrderId.</summary>
    Task<IReadOnlyList<Guid>> FilterSellerOrderIdsByOrderNumberAsync(
        string? op,
        string? value,
        IReadOnlyList<string>? values,
        int take,
        CancellationToken cancellationToken);

    /// <summary>نام گیرنده بر اساس CheckoutId.</summary>
    Task<IReadOnlyDictionary<Guid, string>> GetRecipientNamesAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken);

    /// <summary>جستجوی CheckoutId بر اساس RecipientName.</summary>
    Task<IReadOnlyList<Guid>> SearchCheckoutIdsByRecipientAsync(
        string term,
        int take,
        CancellationToken cancellationToken);

    /// <summary>فیلتر متنی روی RecipientName → CheckoutId.</summary>
    Task<IReadOnlyList<Guid>> FilterCheckoutIdsByRecipientAsync(
        string? op,
        string? value,
        IReadOnlyList<string>? values,
        int take,
        CancellationToken cancellationToken);

    /// <summary>خطوط سفارش برای برچسب محصول/واحد در گرید مرجوعی.</summary>
    Task<IReadOnlyDictionary<Guid, OrderGridLineSnapshot>> GetLinesAsync(
        IReadOnlyList<Guid> orderLineIds,
        CancellationToken cancellationToken);
}

/// <summary>خط سفارش برای گرید مرجوعی.</summary>
public sealed record OrderGridLineSnapshot(
    Guid LineId,
    Guid CatalogVariantId,
    string? UnitCodeSnapshot,
    int QuantityDecimalPlacesSnapshot,
    string? ReturnPolicyLabelSnapshot,
    bool IsReturnableSnapshot,
    int ReturnWindowDaysSnapshot);
