namespace Tooba.Shipping.Domain.Authoring;

/// <summary>
/// گونهٔ مالکِ ذخیره‌شدهٔ یک پیش‌نویس نویسندگی Shipping. این نوع، <b>بازنمایی ماندگاری</b> است و
/// هیچ مجوزی ایجاد نمی‌کند: مالک مجاز از درز سرورساختهٔ TB-189 می‌آید و زیرساخت آن scope را به
/// این نوع نگاشت می‌کند.
/// </summary>
public enum ShippingDraftOwnerKind
{
    /// <summary>فروشگاه SingleStore با Tenant پایدار و بدون فروشنده.</summary>
    Store = 0,

    /// <summary>نصب Marketplace بدون Tenant فروشگاه و بدون فروشنده.</summary>
    Marketplace = 1,

    /// <summary>یک فروشندهٔ مجازشدهٔ Marketplace با SellerPartyId واقعی و بدون Tenant.</summary>
    Seller = 2,
}

/// <summary>
/// کلید مالکِ یک پیش‌نویس نویسندگی: دقیقاً یکی از سه ترکیب مجاز، تغییرناپذیر و فقط از کارخانه‌های
/// خودش ساختنی تا ترکیب ناسازگار (Store بدون Tenant، Marketplace با فروشنده، Seller بدون
/// SellerPartyId) اصلاً قابل ساخت نباشد.
/// </summary>
/// <remarks>
/// این کلید یک <em>فیلتر دسترسی داده</em> است، نه credential. هر پرس‌وجوی خواندن/نوشتن باید با
/// همین کلید فیلتر شود؛ بارگذاری یک ردیف فقط با شناسه و سپس اعتماد به scope فراخوان ممنوع است.
/// </remarks>
public sealed record ShippingDraftOwner
{
    /// <summary>حداکثر طول شناسهٔ پایدار Tenant.</summary>
    public const int TenantIdMaxLength = 128;

    private ShippingDraftOwner(ShippingDraftOwnerKind kind, string? tenantId, Guid? sellerPartyId)
    {
        Kind = kind;
        TenantId = tenantId;
        SellerPartyId = sellerPartyId;
    }

    /// <summary>گونهٔ مالک.</summary>
    public ShippingDraftOwnerKind Kind { get; }

    /// <summary>شناسهٔ پایدار Tenant فقط برای <see cref="ShippingDraftOwnerKind.Store"/>.</summary>
    public string? TenantId { get; }

    /// <summary>SellerPartyId فقط برای <see cref="ShippingDraftOwnerKind.Seller"/>.</summary>
    public Guid? SellerPartyId { get; }

    /// <summary>مالک فروشگاه SingleStore با Tenant پایدار و بدون فروشنده.</summary>
    /// <param name="tenantId">شناسهٔ پایدار Tenant resolve‌شده (نه hostname و نه GUID جعلی).</param>
    public static ShippingDraftOwner ForStore(string tenantId)
    {
        var trimmed = tenantId?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > TenantIdMaxLength)
        {
            throw new ArgumentException("A store draft owner requires a bounded tenant id.", nameof(tenantId));
        }

        return new ShippingDraftOwner(ShippingDraftOwnerKind.Store, trimmed, sellerPartyId: null);
    }

    /// <summary>مالک نصب Marketplace: نه Tenant و نه فروشنده.</summary>
    public static ShippingDraftOwner ForMarketplace() =>
        new(ShippingDraftOwnerKind.Marketplace, tenantId: null, sellerPartyId: null);

    /// <summary>مالک یک فروشندهٔ مجاز با SellerPartyId غیرخالی و بدون Tenant.</summary>
    /// <param name="sellerPartyId">شناسهٔ Party مجازشده توسط درز پلتفرم.</param>
    public static ShippingDraftOwner ForSeller(Guid sellerPartyId)
    {
        if (sellerPartyId == Guid.Empty)
        {
            throw new ArgumentException("A seller draft owner requires a non-empty seller party id.", nameof(sellerPartyId));
        }

        return new ShippingDraftOwner(ShippingDraftOwnerKind.Seller, tenantId: null, sellerPartyId);
    }

    /// <summary>آیا این کلید همان مالک داده‌شده است (مقایسهٔ دقیق، نه فقط گونه).</summary>
    /// <param name="other">کلید دیگر.</param>
    public bool Matches(ShippingDraftOwner other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Kind == other.Kind
               && string.Equals(TenantId, other.TenantId, StringComparison.Ordinal)
               && SellerPartyId == other.SellerPartyId;
    }

    /// <summary>
    /// کلید مالک را از ستون‌های ماندگاری بازمی‌سازد و ترکیب ناسازگار را رد می‌کند. یک ردیف با
    /// ترکیب ناممکن (Store بدون Tenant، Marketplace با فروشنده، …) هرگز یک مالک معتبر نیست.
    /// </summary>
    /// <param name="kind">گونهٔ ذخیره‌شده.</param>
    /// <param name="tenantId">ستون Tenant ذخیره‌شده.</param>
    /// <param name="sellerPartyId">ستون SellerPartyId ذخیره‌شده.</param>
    public static ShippingDraftOwner Restore(
        ShippingDraftOwnerKind kind,
        string? tenantId,
        Guid? sellerPartyId)
        => kind switch
        {
            ShippingDraftOwnerKind.Store when tenantId is not null && sellerPartyId is null =>
                ForStore(tenantId),
            ShippingDraftOwnerKind.Marketplace when tenantId is null && sellerPartyId is null =>
                ForMarketplace(),
            ShippingDraftOwnerKind.Seller when tenantId is null && sellerPartyId is { } seller =>
                ForSeller(seller),
            _ => throw new InvalidOperationException("A persisted draft owner has an impossible owner combination."),
        };
}
