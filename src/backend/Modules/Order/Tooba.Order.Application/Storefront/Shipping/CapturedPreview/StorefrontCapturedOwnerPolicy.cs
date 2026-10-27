using Tooba.BuildingBlocks;
using Tooba.Offer.Contracts.Dtos;
using Tooba.Order.Domain.Checkout;
using Tooba.Shipping.Contracts.Rates;

namespace Tooba.Order.Application.Storefront.Shipping.CapturedPreview;

/// <summary>
/// نتیجهٔ سیاست واحد «چه کسی انجام ارسال این گروه را بر عهده دارد» برای یک گروه ارسال.
/// </summary>
public enum StorefrontCapturedOwnerFailure
{
    /// <summary>مالک resolve شد.</summary>
    None = 0,

    /// <summary>آفر خط هیچ مسئولیت انجام ارسالی نگرفته است.</summary>
    ResponsibilityMissing = 1,

    /// <summary>مقدار مسئولیت خارج از دو خانوادهٔ شناخته‌شده است.</summary>
    ResponsibilityInvalid = 2,

    /// <summary>مسئولیت Seller در نصب SingleStore پشتیبانی نمی‌شود (نه بازتفسیر به Store).</summary>
    SellerUnsupportedInSingleStore = 3,

    /// <summary>زمینهٔ نصب/tenant مورداعتماد برای هیچ مالک پشتیبانی‌شده‌ای کامل نیست.</summary>
    OwnerUnavailable = 4,
}

/// <summary>
/// هویت کامل و <b>تایپ‌شدهٔ</b> مالک ارسال یک گروه: گونه + tenant فروشگاه یا فروشندهٔ واقعی.
/// </summary>
/// <remarks>
/// این مقدار <b>داده</b> است، نه مجوز: از زمینهٔ trusted نصب/tenant به‌همراه مسئولیت صریح انتساب و
/// فروشندهٔ واقعی خطوط همان گروه ساخته می‌شود و هرگز از بدنهٔ مرورگر یا شناسهٔ کاربر خوانده نمی‌شود.
/// </remarks>
/// <param name="Kind">گونهٔ مالک.</param>
/// <param name="TenantId">tenant فروشگاه فقط برای مالک Store یا تهی.</param>
/// <param name="SellerPartyId">فروشندهٔ واقعی فقط برای مالک Seller یا تهی.</param>
public sealed record StorefrontCapturedOwner(
    ShippingCapturedQuoteOwnerKind Kind,
    string? TenantId,
    Guid? SellerPartyId)
{
    /// <summary>
    /// همان هویت به‌شکل مقدار دامنهٔ Order (شاهد فشرده). شکل ناقص/غیرمجاز پیش از هر جهش رد می‌شود.
    /// </summary>
    public ShippingGroupOwner ToDomainOwner() => Kind switch
    {
        ShippingCapturedQuoteOwnerKind.Store => ShippingGroupOwner.Store(TenantId ?? string.Empty),
        ShippingCapturedQuoteOwnerKind.Marketplace => ShippingGroupOwner.Marketplace(),
        ShippingCapturedQuoteOwnerKind.Seller => ShippingGroupOwner.Seller(SellerPartyId ?? Guid.Empty),
        _ => throw new ContractOperationException(ShippingGroupEvidenceFault.OwnerInvalid),
    };

    /// <summary>
    /// آیا این هویت <b>عیناً</b> همان مالک دامنه است؟ tenant حساس به حروف (Ordinal) و فروشنده دقیق؛
    /// هیچ نگاشت تقریبی/پیش‌فرضی در کار نیست.
    /// </summary>
    /// <param name="owner">مالک ذخیره‌شده یا تهی.</param>
    public bool IsSameAs(ShippingGroupOwner? owner)
    {
        if (owner is null)
        {
            return false;
        }

        if ((int)owner.Kind != (int)Kind)
        {
            return false;
        }

        return Kind switch
        {
            ShippingCapturedQuoteOwnerKind.Store =>
                string.Equals(owner.TenantKey, TenantId, StringComparison.Ordinal)
                && owner.SellerPartyId is null,
            ShippingCapturedQuoteOwnerKind.Marketplace =>
                owner.TenantKey is null && owner.SellerPartyId is null,
            ShippingCapturedQuoteOwnerKind.Seller =>
                owner.TenantKey is null
                && SellerPartyId is Guid seller
                && owner.SellerPartyId == seller,
            _ => false,
        };
    }
}

/// <summary>نتیجهٔ سیاست مالک: یا هویت کامل، یا دلیل پایدار نبود آن.</summary>
/// <param name="Owner">مالک resolve‌شده یا تهی.</param>
/// <param name="Failure">دلیل شکست یا <see cref="StorefrontCapturedOwnerFailure.None"/>.</param>
public sealed record StorefrontCapturedOwnerResolution(
    StorefrontCapturedOwner? Owner,
    StorefrontCapturedOwnerFailure Failure)
{
    /// <summary>مالک resolve‌شده.</summary>
    public StorefrontCapturedOwner Resolved => Owner
        ?? throw new ContractOperationException(ShippingGroupEvidenceFault.OwnerRequired);
}

/// <summary>
/// <b>یک</b> سیاست Order-owned برای هویت مالک ارسال گروه‌های capture‌شده، مشترک بین خواندن پیش‌نمایش،
/// نویسندهٔ انتخاب و پذیرش ثبت نهایی؛ هیچ‌کدام نسخهٔ خودشان را تکرار نمی‌کنند.
/// </summary>
/// <remarks>
/// <para>
/// <b>Derived, never supplied.</b> تنها ورودی‌ها زمینهٔ trusted نصب/tenant، مسئولیت <b>صریح</b> انتساب
/// ۲۲۸ و فروشندهٔ <b>واقعی</b> خطوط همان گروه‌اند. مالک Seller همیشه همان فروشندهٔ گروه است؛ یک
/// انتساب Platform هرگز به فروشندهٔ خط تبدیل نمی‌شود و در SingleStore هرگز یک انتساب Seller به Store
/// بازتفسیر نمی‌شود.
/// </para>
/// <para>
/// <b>No default owner.</b> نصب ناشناخته، tenant غیرفعال/تهی، tenant باقی‌مانده در Marketplace و
/// مسئولیت ناشناخته همه با دلیل صریح رد می‌شوند؛ هیچ مالک پیش‌فرضی ساخته نمی‌شود.
/// </para>
/// </remarks>
public static class StorefrontCapturedOwnerPolicy
{
    /// <summary>
    /// مالک یکی از گروه‌های ارسال را از زمینهٔ trusted و انتساب واقعی همان خطوط resolve می‌کند.
    /// </summary>
    /// <param name="edition">حالت نصب جاری یا تهی (resolve‌نشده).</param>
    /// <param name="tenant">زمینهٔ تجارت جاری یا تهی.</param>
    /// <param name="responsibility">مسئولیت صریح انتساب همان آفر یا تهی.</param>
    /// <param name="groupSellerPartyId">فروشندهٔ واقعی خطوط همان گروه.</param>
    public static StorefrontCapturedOwnerResolution Resolve(
        ToobaEdition? edition,
        TenantContext? tenant,
        OfferFulfillmentResponsibility? responsibility,
        Guid groupSellerPartyId)
    {
        if (responsibility is null)
        {
            return Failure(StorefrontCapturedOwnerFailure.ResponsibilityMissing);
        }

        return edition switch
        {
            ToobaEdition.SingleStore => ResolveStore(tenant, responsibility.Value),
            ToobaEdition.Marketplace => ResolveMarketplace(tenant, responsibility.Value, groupSellerPartyId),
            _ => Failure(StorefrontCapturedOwnerFailure.OwnerUnavailable),
        };
    }

    private static StorefrontCapturedOwnerResolution ResolveStore(
        TenantContext? tenant,
        OfferFulfillmentResponsibility responsibility)
    {
        // نصب SingleStore بدون tenant فعالِ غیرتهی هیچ مالکی ندارد (هیچ مقدار پیش‌فرضی ساخته نمی‌شود).
        var tenantId = tenant is { Status: TenantStatus.Active }
            && !string.IsNullOrWhiteSpace(tenant.TenantId.Value)
                ? tenant.TenantId.Value.Trim()
                : null;
        if (tenantId is null)
        {
            return Failure(StorefrontCapturedOwnerFailure.OwnerUnavailable);
        }

        return responsibility switch
        {
            OfferFulfillmentResponsibility.Platform =>
                Resolved(ShippingCapturedQuoteOwnerKind.Store, tenantId, null),
            // ارسال فروشنده در SingleStore پشتیبانی نمی‌شود و هرگز به Store بازتفسیر نمی‌شود.
            OfferFulfillmentResponsibility.Seller =>
                Failure(StorefrontCapturedOwnerFailure.SellerUnsupportedInSingleStore),
            _ => Failure(StorefrontCapturedOwnerFailure.ResponsibilityInvalid),
        };
    }

    private static StorefrontCapturedOwnerResolution ResolveMarketplace(
        TenantContext? tenant,
        OfferFulfillmentResponsibility responsibility,
        Guid groupSellerPartyId)
    {
        // Marketplace هیچ tenant فروشگاهی ندارد: یک tenant باقی‌مانده زمینهٔ نامعتبر است، نه حدس.
        if (tenant is not null)
        {
            return Failure(StorefrontCapturedOwnerFailure.OwnerUnavailable);
        }

        return responsibility switch
        {
            OfferFulfillmentResponsibility.Platform =>
                Resolved(ShippingCapturedQuoteOwnerKind.Marketplace, null, null),
            OfferFulfillmentResponsibility.Seller when groupSellerPartyId != Guid.Empty =>
                Resolved(ShippingCapturedQuoteOwnerKind.Seller, null, groupSellerPartyId),
            OfferFulfillmentResponsibility.Seller =>
                Failure(StorefrontCapturedOwnerFailure.OwnerUnavailable),
            _ => Failure(StorefrontCapturedOwnerFailure.ResponsibilityInvalid),
        };
    }

    private static StorefrontCapturedOwnerResolution Resolved(
        ShippingCapturedQuoteOwnerKind kind,
        string? tenantId,
        Guid? sellerPartyId) =>
        new(new StorefrontCapturedOwner(kind, tenantId, sellerPartyId), StorefrontCapturedOwnerFailure.None);

    private static StorefrontCapturedOwnerResolution Failure(StorefrontCapturedOwnerFailure failure) =>
        new(null, failure);
}
