namespace Tooba.Shipping.Application.Authoring.Models;

/// <summary>
/// محدودهٔ مالکِ مجازشدهٔ قواعد ارسال برای همین درخواست — تغییرناپذیر و فقط ساختنی از مسیر
/// کارخانه‌های خودش، تا ترکیب‌های ناسازگار (Store با فروشنده، Marketplace با Tenant، Seller بدون
/// SellerPartyId) اصلاً قابل ساخت نباشد.
/// </summary>
/// <remarks>
/// <para>
/// این مدل <b>دادهٔ</b> یک مالک resolve‌شده است، نه مجوز به‌خاطر نام نوعش: هر خواندن/نوشتن آینده
/// باید مجوز همان عملیات را دوباره از سمت سرور بگیرد. این scope یک credential، grant یا توکن
/// نوشتن قابل استفادهٔ مجدد نیست.
/// </para>
/// <para>
/// هویت actor، حالت نصب و برچسب نمایشی عمداً اینجا نیستند؛ آن‌ها در مجوزدهی بررسی می‌شوند.
/// شناسهٔ پایدار Tenant فقط برای scope داخلی است و هرگز در HTTP پاسخ داده نمی‌شود.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringOwnerScope
{
    private ShippingAuthoringOwnerScope(
        ShippingAuthoringOwnerKind kind,
        string? tenantId,
        Guid? sellerPartyId)
    {
        Kind = kind;
        TenantId = tenantId;
        SellerPartyId = sellerPartyId;
    }

    /// <summary>گونهٔ مالک مجازشده.</summary>
    public ShippingAuthoringOwnerKind Kind { get; }

    /// <summary>
    /// شناسهٔ پایدار Tenant فروشگاه در scope <see cref="ShippingAuthoringOwnerKind.Store"/>؛
    /// برای Marketplace و Seller همیشه <c>null</c>. این مقدار داخلی است و به کلاینت نمی‌رود.
    /// </summary>
    public string? TenantId { get; }

    /// <summary>
    /// SellerPartyId واقعیِ مجازشده فقط در scope <see cref="ShippingAuthoringOwnerKind.Seller"/>;
    /// برای Store و Marketplace همیشه <c>null</c>.
    /// </summary>
    public Guid? SellerPartyId { get; }

    /// <summary>
    /// scope مالک فروشگاه SingleStore: TenantId پایدار الزامی است و هیچ فروشنده‌ای ندارد.
    /// </summary>
    /// <param name="tenantId">شناسهٔ پایدار Tenant فعال resolve‌شده (نه hostname و نه GUID جعلی).</param>
    public static ShippingAuthoringOwnerScope ForStore(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("A store owner scope requires the resolved tenant id.", nameof(tenantId));
        }

        return new ShippingAuthoringOwnerScope(
            ShippingAuthoringOwnerKind.Store,
            tenantId.Trim(),
            sellerPartyId: null);
    }

    /// <summary>scope مالک نصب Marketplace: نه Tenant و نه فروشنده.</summary>
    public static ShippingAuthoringOwnerScope ForMarketplace() =>
        new(ShippingAuthoringOwnerKind.Marketplace, tenantId: null, sellerPartyId: null);

    /// <summary>
    /// scope مالک یک فروشندهٔ مجاز: SellerPartyId غیرخالی الزامی است و هیچ Tenant فروشگاهی ندارد.
    /// مجاز بودن این فروشنده در Marketplace وظیفهٔ مجوزدهی سرور است، نه این مدل.
    /// </summary>
    /// <param name="sellerPartyId">شناسهٔ Party فروشندهٔ مجازشده توسط درز پلتفرم.</param>
    public static ShippingAuthoringOwnerScope ForSeller(Guid sellerPartyId)
    {
        if (sellerPartyId == Guid.Empty)
        {
            throw new ArgumentException("A seller owner scope requires a non-empty seller party id.", nameof(sellerPartyId));
        }

        return new ShippingAuthoringOwnerScope(
            ShippingAuthoringOwnerKind.Seller,
            tenantId: null,
            sellerPartyId);
    }
}
