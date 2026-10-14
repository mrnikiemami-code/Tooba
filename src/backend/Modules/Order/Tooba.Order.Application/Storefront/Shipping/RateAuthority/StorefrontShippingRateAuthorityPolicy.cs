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
    {
        if (options is null || !options.ShadowEnabled)
        {
            return StorefrontShippingRateAuthorityGate.Disabled;
        }

        return options.PrerequisitesConfirmed
            ? StorefrontShippingRateAuthorityGate.Allowed
            : StorefrontShippingRateAuthorityGate.PrerequisitesUnconfirmed;
    }
}
