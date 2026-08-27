using Tooba.BuildingBlocks;

namespace Tooba.Promotion.Domain;

/// <summary>
/// وضعیت انتشار پروموشن. پیش‌نویس در ارزیابی شرکت نمی‌کند.
/// </summary>
public enum PromotionStatus
{
    /// <summary>
    /// هنوز برای تسویه قابل‌اعمال نیست.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// در پنجرهٔ اعتبار می‌تواند ارزیابی شود.
    /// </summary>
    Active = 1,

    /// <summary>
    /// منقضی یا بازنشسته؛ سفارش تاریخی را عوض نمی‌کند.
    /// </summary>
    Expired = 2,
}

/// <summary>
/// سیاست ترکیب با سایر پروموشن‌های منطبق.
/// </summary>
public enum PromotionStackingPolicy
{
    /// <summary>
    /// با سایر Stackableها به ترتیب اولویت قطعی جمع می‌شود.
    /// </summary>
    Stackable = 0,

    /// <summary>
    /// با هیچ پروموشن دیگری جمع نمی‌شود؛ بین چند Exclusive برنده با اولویت سپس شناسه انتخاب می‌شود.
    /// </summary>
    Exclusive = 1,
}

/// <summary>
/// گونهٔ عمل تخفیف. تخفیف جایگزین قیمت تألیف‌شدهٔ Pricing نیست.
/// </summary>
public enum PromotionDiscountKind
{
    /// <summary>
    /// درصد از مبلغ بدون مالیات خط.
    /// </summary>
    PercentageOff = 0,

    /// <summary>
    /// مبلغ ثابت با ارز صریح.
    /// </summary>
    FixedAmountOff = 1,
}

/// <summary>
/// گرد کردن قطعی مبلغ تخفیف طبق مقیاس ارز. ممیز شناور نیست.
/// </summary>
public static class PromotionRounding
{
    /// <summary>
    /// IRR/JPY/KRW بدون اعشار؛ بقیه دو رقم. Midpoint AwayFromZero.
    /// </summary>
    public static decimal Round(decimal amount, string currency)
    {
        var scale = string.Equals(currency, "IRR", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(currency, "JPY", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(currency, "KRW", StringComparison.OrdinalIgnoreCase)
            ? 0
            : 2;
        return decimal.Round(amount, scale, MidpointRounding.AwayFromZero);
    }
}

/// <summary>
/// نرمال‌سازی کد کوپن. داشتن کد به‌تنهایی مجوز اعمال نیست.
/// </summary>
public static class PromotionCouponNormalizer
{
    /// <summary>
    /// فاصله‌ها را می‌زداید و حروف راInvariant بزرگ می‌کند تا مقایسه قطعی باشد.
    /// </summary>
    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return string.Empty;
        }

        return string.Join(
            "",
            code.Trim().ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }
}

/// <summary>
/// پروموشن شرطی. قیمت پایهٔ Pricing را بازنویسی نمی‌کند و مالیات حساب نمی‌کند.
/// </summary>
public sealed class PromotionDefinition : IHasDomainEvents
{
    private readonly DomainEventCollector _domainEvents = new();

    /// <summary>
    /// سازندهٔ EF.
    /// </summary>
    private PromotionDefinition()
    {
    }

    /// <summary>
    /// شناسهٔ پایدار پروموشن.
    /// </summary>
    public Guid PromotionId { get; init; }

    /// <summary>
    /// نام عملیاتی؛ محتوای بازاریابی نیست.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// وضعیت انتشار.
    /// </summary>
    public PromotionStatus Status { get; private set; }

    /// <summary>
    /// اولویت قطعی؛ عدد بزرگ‌تر زودتر اعمال می‌شود. ترتیب ردیف دیتابیس ملاک نیست.
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// شروع اعتبار UTC.
    /// </summary>
    public DateTimeOffset EffectiveFrom { get; private set; }

    /// <summary>
    /// پایان اعتبار اختیاری UTC؛ مقدار تهی یعنی باز.
    /// </summary>
    public DateTimeOffset? EffectiveTo { get; private set; }

    /// <summary>
    /// سیاست ترکیب.
    /// </summary>
    public PromotionStackingPolicy StackingPolicy { get; private set; }

    /// <summary>
    /// گونهٔ تخفیف.
    /// </summary>
    public PromotionDiscountKind DiscountKind { get; private set; }

    /// <summary>
    /// نرخ کسری درصد (مثلاً ۰٫۱۰). برای مبلغ ثابت صفر است.
    /// </summary>
    public decimal PercentageRate { get; private set; }

    /// <summary>
    /// مبلغ ثابت. برای درصد صفر است.
    /// </summary>
    public decimal FixedAmount { get; private set; }

    /// <summary>
    /// ارز مبلغ ثابت؛ برای درصد تهی است.
    /// </summary>
    public string? FixedAmountCurrency { get; private set; }

    /// <summary>
    /// کد کوپن نرمال‌شدهٔ اختیاری. تهی یعنی اعمال خودکار در صورت احراز صلاحیت.
    /// </summary>
    public string? CouponCode { get; private set; }

    /// <summary>
    /// محدودکنندهٔ Offer؛ تهی یعنی این محور فیلتر نمی‌شود.
    /// </summary>
    public Guid? OfferId { get; private set; }

    /// <summary>
    /// محدودکنندهٔ گونهٔ کاتالوگ.
    /// </summary>
    public Guid? CatalogVariantId { get; private set; }

    /// <summary>
    /// محدودکنندهٔ طبقه.
    /// </summary>
    public Guid? CategoryId { get; private set; }

    /// <summary>
    /// محدودکنندهٔ فروشنده.
    /// </summary>
    public Guid? SellerPartyId { get; private set; }

    /// <summary>
    /// بازار تجاری.
    /// </summary>
    public string? Market { get; private set; }

    /// <summary>
    /// کانال فروش به‌صورت متن پایدار.
    /// </summary>
    public string? SalesChannel { get; private set; }

    /// <summary>
    /// ارز صلاحیت.
    /// </summary>
    public string? Currency { get; private set; }

    /// <summary>
    /// مشتری اختیاری.
    /// </summary>
    public Guid? CustomerPartyId { get; private set; }

    /// <summary>
    /// سازمان اختیاری.
    /// </summary>
    public Guid? OrganizationPartyId { get; private set; }

    /// <summary>
    /// حداقل تعداد.
    /// </summary>
    public int? MinimumQuantity { get; private set; }

    /// <summary>
    /// حداقل جمع بدون مالیات خط.
    /// </summary>
    public decimal? MinimumSubtotal { get; private set; }

    /// <summary>
    /// ایجاد UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// به‌روزرسانی UTC.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.Events;

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <summary>
    /// پروموشن پیش‌نویس می‌سازد. منطق کمپین در کنترلر نیست.
    /// </summary>
    public static PromotionDefinition Create(
        string name,
        int priority,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveTo,
        PromotionStackingPolicy stackingPolicy,
        PromotionDiscountKind discountKind,
        decimal percentageRate,
        decimal fixedAmount,
        string? fixedAmountCurrency,
        string? couponCode,
        Guid? offerId,
        Guid? catalogVariantId,
        Guid? categoryId,
        Guid? sellerPartyId,
        string? market,
        string? salesChannel,
        string? currency,
        Guid? customerPartyId,
        Guid? organizationPartyId,
        int? minimumQuantity,
        decimal? minimumSubtotal,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("نام پروموشن خالی نیست.");
        }

        if (effectiveTo is not null && effectiveTo <= effectiveFrom)
        {
            throw new InvalidOperationException("پنجرهٔ اعتبار پروموشن نامعتبر است.");
        }

        if (discountKind == PromotionDiscountKind.PercentageOff)
        {
            if (percentageRate <= 0 || percentageRate > 1)
            {
                throw new InvalidOperationException("نرخ درصدی باید کسری بین صفر و یک باشد.");
            }

            if (fixedAmount != 0)
            {
                throw new InvalidOperationException("پروموشن درصدی مبلغ ثابت ندارد.");
            }
        }
        else
        {
            if (fixedAmount <= 0)
            {
                throw new InvalidOperationException("مبلغ ثابت باید مثبت باشد.");
            }

            if (string.IsNullOrWhiteSpace(fixedAmountCurrency))
            {
                throw new InvalidOperationException("مبلغ ثابت بدون ارز اعمال نمی‌شود.");
            }

            if (percentageRate != 0)
            {
                throw new InvalidOperationException("پروموشن مبلغ ثابت نرخ درصد ندارد.");
            }
        }

        if (minimumQuantity is <= 0)
        {
            throw new InvalidOperationException("حداقل تعداد باید مثبت باشد.");
        }

        if (minimumSubtotal is < 0)
        {
            throw new InvalidOperationException("حداقل جمع منفی نیست.");
        }

        var promotion = new PromotionDefinition
        {
            PromotionId = UuidV7.New(),
            Name = name.Trim(),
            Status = PromotionStatus.Draft,
            Priority = priority,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            StackingPolicy = stackingPolicy,
            DiscountKind = discountKind,
            PercentageRate = percentageRate,
            FixedAmount = fixedAmount,
            FixedAmountCurrency = string.IsNullOrWhiteSpace(fixedAmountCurrency)
                ? null
                : fixedAmountCurrency.Trim().ToUpperInvariant(),
            CouponCode = string.IsNullOrWhiteSpace(couponCode)
                ? null
                : PromotionCouponNormalizer.Normalize(couponCode),
            OfferId = offerId,
            CatalogVariantId = catalogVariantId,
            CategoryId = categoryId,
            SellerPartyId = sellerPartyId,
            Market = string.IsNullOrWhiteSpace(market) ? null : market.Trim(),
            SalesChannel = string.IsNullOrWhiteSpace(salesChannel) ? null : salesChannel.Trim(),
            Currency = string.IsNullOrWhiteSpace(currency) ? null : currency.Trim().ToUpperInvariant(),
            CustomerPartyId = customerPartyId,
            OrganizationPartyId = organizationPartyId,
            MinimumQuantity = minimumQuantity,
            MinimumSubtotal = minimumSubtotal,
            CreatedAt = now,
            UpdatedAt = now,
        };
        promotion._domainEvents.Add(new PromotionCreatedDomainEvent(promotion.PromotionId));
        return promotion;
    }

    /// <summary>
    /// پروموشن را برای ارزیابی فعال می‌کند.
    /// </summary>
    public void Activate(DateTimeOffset now)
    {
        Status = PromotionStatus.Active;
        UpdatedAt = now;
        _domainEvents.Add(new PromotionActivatedDomainEvent(PromotionId));
    }

    /// <summary>
    /// نام یا اولویت را عوض می‌کند. تصویر سفارش قبلی بازنویسی نمی‌شود.
    /// </summary>
    public void Change(string name, int priority, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("نام پروموشن خالی نیست.");
        }

        Name = name.Trim();
        Priority = priority;
        UpdatedAt = now;
        _domainEvents.Add(new PromotionChangedDomainEvent(PromotionId));
    }

    /// <summary>
    /// فیلدهای پیش‌نویس یا منقضی را به‌روز می‌کند. پروموشن Active قابل ویرایش اقتصادی نیست.
    /// </summary>
    public void UpdateEditableFields(
        string name,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveTo,
        PromotionDiscountKind discountKind,
        decimal percentageRate,
        decimal fixedAmount,
        string? fixedAmountCurrency,
        string? couponCode,
        decimal? minimumSubtotal,
        DateTimeOffset now)
    {
        if (Status == PromotionStatus.Active)
        {
            throw new InvalidOperationException("پروموشن فعال قابل ویرایش فیلدهای اقتصادی نیست.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("نام پروموشن خالی نیست.");
        }

        if (effectiveTo is not null && effectiveTo <= effectiveFrom)
        {
            throw new InvalidOperationException("پنجرهٔ اعتبار پروموشن نامعتبر است.");
        }

        if (discountKind == PromotionDiscountKind.PercentageOff)
        {
            if (percentageRate <= 0 || percentageRate > 1)
            {
                throw new InvalidOperationException("نرخ درصدی باید کسری بین صفر و یک باشد.");
            }

            if (fixedAmount != 0)
            {
                throw new InvalidOperationException("پروموشن درصدی مبلغ ثابت ندارد.");
            }
        }
        else
        {
            if (fixedAmount <= 0)
            {
                throw new InvalidOperationException("مبلغ ثابت باید مثبت باشد.");
            }

            if (string.IsNullOrWhiteSpace(fixedAmountCurrency))
            {
                throw new InvalidOperationException("مبلغ ثابت بدون ارز اعمال نمی‌شود.");
            }

            if (percentageRate != 0)
            {
                throw new InvalidOperationException("پروموشن مبلغ ثابت نرخ درصد ندارد.");
            }
        }

        if (minimumSubtotal is < 0)
        {
            throw new InvalidOperationException("حداقل جمع منفی نیست.");
        }

        Name = name.Trim();
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        DiscountKind = discountKind;
        PercentageRate = percentageRate;
        FixedAmount = fixedAmount;
        FixedAmountCurrency = string.IsNullOrWhiteSpace(fixedAmountCurrency)
            ? null
            : fixedAmountCurrency.Trim().ToUpperInvariant();
        CouponCode = string.IsNullOrWhiteSpace(couponCode)
            ? null
            : PromotionCouponNormalizer.Normalize(couponCode);
        MinimumSubtotal = minimumSubtotal;
        UpdatedAt = now;
        _domainEvents.Add(new PromotionChangedDomainEvent(PromotionId));
    }

    /// <summary>
    /// پروموشن را منقضی می‌کند. سفارش‌های ثبت‌شده را لمس نمی‌کند.
    /// </summary>
    public void Expire(DateTimeOffset now)
    {
        Status = PromotionStatus.Expired;
        UpdatedAt = now;
        _domainEvents.Add(new PromotionExpiredDomainEvent(PromotionId));
    }

    /// <summary>
    /// آیا لحظه داخل پنجره و وضعیت Active است.
    /// </summary>
    public bool IsEffectiveAt(DateTimeOffset at) =>
        Status == PromotionStatus.Active
        && at >= EffectiveFrom
        && (EffectiveTo is null || at < EffectiveTo);

    /// <summary>
    /// صلاحیت محورها را روی واقعیت‌های ورودی قرارداد می‌سنجد؛ DbContext خارجی خوانده نمی‌شود.
    /// </summary>
    public bool IsEligible(PromotionEligibilityFacts facts)
    {
        if (OfferId is not null && OfferId != facts.OfferId)
        {
            return false;
        }

        if (CatalogVariantId is not null && CatalogVariantId != facts.CatalogVariantId)
        {
            return false;
        }

        if (CategoryId is not null && CategoryId != facts.CategoryId)
        {
            return false;
        }

        if (SellerPartyId is not null && SellerPartyId != facts.SellerPartyId)
        {
            return false;
        }

        if (Market is not null && !string.Equals(Market, facts.Market, StringComparison.Ordinal))
        {
            return false;
        }

        if (SalesChannel is not null && !string.Equals(SalesChannel, facts.SalesChannel, StringComparison.Ordinal))
        {
            return false;
        }

        if (Currency is not null && !string.Equals(Currency, facts.Currency, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (CustomerPartyId is not null && CustomerPartyId != facts.CustomerPartyId)
        {
            return false;
        }

        if (OrganizationPartyId is not null && OrganizationPartyId != facts.OrganizationPartyId)
        {
            return false;
        }

        if (MinimumQuantity is not null && facts.Quantity < MinimumQuantity)
        {
            return false;
        }

        if (MinimumSubtotal is not null && facts.BaseTaxExclusiveAmount < MinimumSubtotal)
        {
            return false;
        }

        if (CouponCode is null)
        {
            return true;
        }

        return string.Equals(CouponCode, PromotionCouponNormalizer.Normalize(facts.CouponCode), StringComparison.Ordinal);
    }

    /// <summary>
    /// مبلغ تخفیف را از پایهٔ بدون مالیات حساب می‌کند. سقف باقیمانده از منفی شدن جلوگیری می‌کند.
    /// </summary>
    public decimal ComputeDiscount(decimal remainingExclusive, string currency)
    {
        if (remainingExclusive <= 0)
        {
            return 0m;
        }

        decimal raw;
        if (DiscountKind == PromotionDiscountKind.PercentageOff)
        {
            raw = remainingExclusive * PercentageRate;
        }
        else
        {
            if (!string.Equals(FixedAmountCurrency, currency, StringComparison.OrdinalIgnoreCase))
            {
                return 0m;
            }

            raw = FixedAmount;
        }

        var rounded = PromotionRounding.Round(raw, currency);
        return rounded > remainingExclusive ? remainingExclusive : rounded;
    }
}

/// <summary>
/// واقعیت‌های صلاحیت که از قرارداد/تصویر Checkout می‌آید نه از JOIN به schemaهای بیگانه.
/// </summary>
public sealed record PromotionEligibilityFacts(
    Guid OfferId,
    Guid CatalogVariantId,
    Guid? CategoryId,
    Guid SellerPartyId,
    string Market,
    string SalesChannel,
    string Currency,
    Guid? CustomerPartyId,
    Guid? OrganizationPartyId,
    int Quantity,
    decimal BaseTaxExclusiveAmount,
    string? CouponCode);

/// <summary>
/// ایجاد پروموشن.
/// </summary>
public sealed class PromotionCreatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PromotionCreatedDomainEvent(Guid promotionId)
    {
        PromotionId = promotionId;
        Metadata = EventMetadataFactory.ForDomain("promotion.created.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پروموشن.
    /// </summary>
    public Guid PromotionId { get; }
}

/// <summary>
/// فعال‌سازی پروموشن.
/// </summary>
public sealed class PromotionActivatedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PromotionActivatedDomainEvent(Guid promotionId)
    {
        PromotionId = promotionId;
        Metadata = EventMetadataFactory.ForDomain("promotion.activated.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پروموشن.
    /// </summary>
    public Guid PromotionId { get; }
}

/// <summary>
/// تغییر تعریف. تصویر سفارش را عوض نمی‌کند.
/// </summary>
public sealed class PromotionChangedDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PromotionChangedDomainEvent(Guid promotionId)
    {
        PromotionId = promotionId;
        Metadata = EventMetadataFactory.ForDomain("promotion.changed.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پروموشن.
    /// </summary>
    public Guid PromotionId { get; }
}

/// <summary>
/// انقضای پروموشن.
/// </summary>
public sealed class PromotionExpiredDomainEvent : IDomainEvent
{
    /// <summary>
    /// رویداد را می‌سازد.
    /// </summary>
    public PromotionExpiredDomainEvent(Guid promotionId)
    {
        PromotionId = promotionId;
        Metadata = EventMetadataFactory.ForDomain("promotion.expired.v1");
    }

    /// <inheritdoc />
    public EventMetadata Metadata { get; }

    /// <summary>
    /// پروموشن.
    /// </summary>
    public Guid PromotionId { get; }
}
