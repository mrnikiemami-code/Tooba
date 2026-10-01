namespace Tooba.BuildingBlocks.Presentation.Errors;

/// <summary>
/// کدهای خطای عرضی مشترک (نشست مشتری و مجوز نقش‌ها) که مالکیت توصیف‌گر کاتالوگ آن‌ها در
/// لایهٔ foundation است. ماژول‌ها ممکن است همین کد ماشین را مصرف کنند، اما توصیف‌گر canonical
/// را فقط یک بار — همین‌جا — ثبت می‌شود تا کاتالوگ ترکیبی تک‌مالک بماند.
/// </summary>
public static class FoundationErrorCodes
{
    /// <summary>نشست معتبر مشتری برای مسیر لازم است (401).</summary>
    public const string CustomerSessionRequired = "customer.session.required";

    /// <summary>شروع/ادامهٔ فرایند خرید مستلزم ورود مشتری است (401).</summary>
    public const string CheckoutAuthenticationRequired = "checkout.authentication_required";

    /// <summary>مجوز فروشنده برای این عملیات رد شد (403).</summary>
    public const string SellerAuthorizationDenied = "seller.authorization.denied";

    /// <summary>مجوز مدیر برای این عملیات رد شد (403).</summary>
    public const string AdminAuthorizationDenied = "admin.authorization.denied";

    /// <summary>هویت مدیر احراز نشده است (401).</summary>
    public const string AdminActorMissing = "admin.actor.missing";

    /// <summary>زمینهٔ Tenant/فروشگاه برای پنل مدیر موجود نیست (503).</summary>
    public const string AdminTenantMissing = "admin.tenant.missing";

    /// <summary>زیرساخت مجوز برای پنل مدیر در دسترس نیست (503).</summary>
    public const string AdminAuthorizationUnavailable = "admin.authorization.unavailable";

    /// <summary>زمینهٔ Development پنل مدیر در دسترس نیست (404).</summary>
    public const string AdminDevUnavailable = "admin.dev.unavailable";

    /// <summary>Edition پلتفرم پیکربندی نشده است (503).</summary>
    public const string PlatformEditionUnconfigured = "platform.edition.unconfigured";

    /// <summary>مرجع اتصال پلتفرم پیکربندی نشده است (503).</summary>
    public const string PlatformConnectionUnconfigured = "platform.connection.unconfigured";

    /// <summary>Resolve Host/Tenant با شکست fail-closed مواجه شد (404؛ بدون افشای وجود).</summary>
    public const string PlatformResolutionFailed = "platform.resolution.failed";
}
