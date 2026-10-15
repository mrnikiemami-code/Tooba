namespace Tooba.Order.Application.Storefront.Shipping.RateAuthority;

/// <summary>نتیجهٔ ارزیابی دروازهٔ مرجع نرخ ارسال.</summary>
public enum StorefrontShippingRateAuthorityGate
{
    /// <summary>سایه خاموش است؛ هیچ مقایسه‌ای اجرا نمی‌شود (پیش‌فرض امن).</summary>
    Disabled = 0,

    /// <summary>سایه روشن است اما پیش‌نیازها تأیید نشده‌اند؛ مقایسه اجرا نمی‌شود.</summary>
    PrerequisitesUnconfirmed = 1,

    /// <summary>هر دو شرط برقرار است؛ مقایسهٔ سایه مجاز است (فقط مشاهده، بدون تغییر قیمت).</summary>
    Allowed = 2,
}

/// <summary>
/// دروازهٔ صریح برای مجاز بودن مقایسهٔ نرخ ارسال. هیچ مسیر دیگری در این درز مجاز نیست.
/// </summary>
/// <remarks>
/// این دروازه دو شرط را <em>با هم</em> لازم می‌داند و هر کمبودی را به «اجرا نکن» ترجمه می‌کند، نه به
/// «پیش‌فرض مجاز». به‌طور خاص: نبود schema یا نبود مجوز پوشش هرگز به معنای «بدون محدودیت» نیست و
/// هرگز به معنای «قیمت جدید را بپذیر» هم نیست.
/// </remarks>
public static class StorefrontShippingRateAuthorityPolicy
{
    /// <summary>وضعیت دروازه را بر پایهٔ تنظیمات تعیین می‌کند.</summary>
    /// <param name="options">تنظیمات؛ تهی یعنی خاموش.</param>
    public static StorefrontShippingRateAuthorityGate Evaluate(StorefrontShippingRateAuthorityOptions? options)
        => Evaluate(options?.ShadowEnabled ?? false, options?.PrerequisitesConfirmed ?? false);

    /// <summary>
    /// همان سیاست fail-closed برای هر درز سایهٔ دیگری که همان دو شرط را لازم دارد (مثلاً سایهٔ
    /// عملیاتی ارسال). این overload <em>هیچ</em> رفتار تازه‌ای اضافه نمی‌کند: فقط همان قاعده را
    /// روی دو مقدار صریح اعمال می‌کند تا معنای درز قبلی گسترده نشود.
    /// </summary>
    /// <param name="shadowEnabled">آیا سایه روشن است.</param>
    /// <param name="prerequisitesConfirmed">آیا پیش‌نیازهای واقعی تأیید شده‌اند.</param>
    public static StorefrontShippingRateAuthorityGate Evaluate(bool shadowEnabled, bool prerequisitesConfirmed)
    {
        if (!shadowEnabled)
        {
            return StorefrontShippingRateAuthorityGate.Disabled;
        }

        return prerequisitesConfirmed
            ? StorefrontShippingRateAuthorityGate.Allowed
            : StorefrontShippingRateAuthorityGate.PrerequisitesUnconfirmed;
    }
}
