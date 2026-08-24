namespace Tooba.Host.Storefront;

/// <summary>
/// انتخاب موقت Offer نمایشی تا Buy Box کاننیکال ساخته شود.
/// قاعده: فقط کاندیداهای دارای قیمت فعال؛ اولویت با موجودی قابل‌فروش مثبت، سپس کمترین مبلغ بدون مالیات، سپس OfferId.
/// این قانون حدس فرانت‌اند یا first-row-wins نیست و روی هویت Product قیمت نمی‌نویسد.
/// </summary>
public static class StorefrontPrimaryOfferResolver
{
    /// <summary>
    /// Offer نمایشی را از کاندیداهای ازپیش‌ترکیب‌شده برمی‌گرداند.
    /// </summary>
    /// <param name="candidates">کاندیداهایی که قیمت‌شان از Pricing آمده است.</param>
    /// <returns>Offer منتخب یا تهی اگر قیمت فعالی نباشد.</returns>
    public static StorefrontOfferCandidate? Resolve(IReadOnlyList<StorefrontOfferCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        return candidates
            .OrderByDescending(candidate => candidate.AvailableUnits > 0)
            .ThenBy(candidate => candidate.AmountExclusiveOfTax)
            .ThenBy(candidate => candidate.OfferId)
            .First();
    }
}
