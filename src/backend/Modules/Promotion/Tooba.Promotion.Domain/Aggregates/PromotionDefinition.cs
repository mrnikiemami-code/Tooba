using Tooba.Promotion.Domain.Policies;
using Tooba.BuildingBlocks;
using Tooba.Promotion.Domain.ValueObjects;
using Tooba.Promotion.Domain.Events;

namespace Tooba.Promotion.Domain.Aggregates;

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
        Guid promotionId,
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
        if (promotionId == Guid.Empty)
        {
            throw new InvalidOperationException("promotion.definition.id_required");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("promotion.definition.name_required");
        }

        if (effectiveTo is not null && effectiveTo <= effectiveFrom)
        {
            throw new InvalidOperationException("promotion.definition.window_invalid");
        }

        if (discountKind == PromotionDiscountKind.PercentageOff)
        {
            if (percentageRate <= 0 || percentageRate > 1)
            {
                throw new InvalidOperationException("promotion.definition.percent_invalid");
            }

            if (fixedAmount != 0)
            {
                throw new InvalidOperationException("promotion.definition.percent_no_fixed");
            }
        }
        else
        {
            if (fixedAmount <= 0)
            {
                throw new InvalidOperationException("promotion.definition.fixed_amount_invalid");
            }

            if (string.IsNullOrWhiteSpace(fixedAmountCurrency))
            {
                throw new InvalidOperationException("promotion.definition.fixed_currency_required");
            }

            if (percentageRate != 0)
            {
                throw new InvalidOperationException("promotion.definition.fixed_no_percent");
            }
        }

        if (minimumQuantity is <= 0)
        {
            throw new InvalidOperationException("promotion.definition.min_qty_invalid");
        }

        if (minimumSubtotal is < 0)
        {
            throw new InvalidOperationException("promotion.definition.min_subtotal_invalid");
        }

        var promotion = new PromotionDefinition
        {
            PromotionId = promotionId,
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
            throw new InvalidOperationException("promotion.definition.name_required");
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
            throw new InvalidOperationException("promotion.definition.active_immutable");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("promotion.definition.name_required");
        }

        if (effectiveTo is not null && effectiveTo <= effectiveFrom)
        {
            throw new InvalidOperationException("promotion.definition.window_invalid");
        }

        if (discountKind == PromotionDiscountKind.PercentageOff)
        {
            if (percentageRate <= 0 || percentageRate > 1)
            {
                throw new InvalidOperationException("promotion.definition.percent_invalid");
            }

            if (fixedAmount != 0)
            {
                throw new InvalidOperationException("promotion.definition.percent_no_fixed");
            }
        }
        else
        {
            if (fixedAmount <= 0)
            {
                throw new InvalidOperationException("promotion.definition.fixed_amount_invalid");
            }

            if (string.IsNullOrWhiteSpace(fixedAmountCurrency))
            {
                throw new InvalidOperationException("promotion.definition.fixed_currency_required");
            }

            if (percentageRate != 0)
            {
                throw new InvalidOperationException("promotion.definition.fixed_no_percent");
            }
        }

        if (minimumSubtotal is < 0)
        {
            throw new InvalidOperationException("promotion.definition.min_subtotal_invalid");
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
    public decimal ComputeDiscount(
        decimal remainingExclusive,
        string currency,
        QuantityRoundingMode roundingMode = QuantityRoundingMode.Nearest)
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

        var rounded = PromotionRounding.Round(raw, currency, roundingMode);
        return rounded > remainingExclusive ? remainingExclusive : rounded;
    }
}
