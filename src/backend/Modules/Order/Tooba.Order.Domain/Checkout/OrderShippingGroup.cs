using Tooba.BuildingBlocks;

namespace Tooba.Order.Domain.Checkout;

/// <summary>
/// Immutable accepted-order shipping group: one seller, one canonical method, one accepted charge.
/// It is materialized ONCE at the order-conversion boundary and never rewritten afterwards.
/// </summary>
public sealed class OrderShippingGroup
{
    /// <summary>وضعیت پایدار پس از ثبت سفارش.</summary>
    public const string ConfirmedState = "Confirmed";

    /// <summary>سازندهٔ EF.</summary>
    private OrderShippingGroup()
    {
    }

    /// <summary>شناسهٔ پایدار گروه؛ همان هویت پیش‌نویس M1.</summary>
    public Guid ShippingGroupId { get; init; }

    /// <summary>سفارش مشتری مالک.</summary>
    public Guid CheckoutId { get; init; }

    /// <summary>سفارش فروشندهٔ مالک گروه.</summary>
    public Guid SellerOrderId { get; init; }

    /// <summary>فروشندهٔ گروه.</summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>کد روش متعارف.</summary>
    public string MethodCode { get; private set; } = string.Empty;

    /// <summary>کد سرویس؛ فیلد جداگانه.</summary>
    public string? ServiceCode { get; private set; }

    /// <summary>کد گزینه؛ فیلد جداگانه.</summary>
    public string? OptionCode { get; private set; }

    /// <summary>برچسب snapshot؛ فقط ارائه.</summary>
    public string MethodLabel { get; private set; } = string.Empty;

    /// <summary>
    /// بافت مبدأ؛ تا وجود مرجع مورداعتماد همیشه null است و هرگز جعل نمی‌شود.
    /// </summary>
    public string? OriginContext { get; private set; }

    /// <summary>هزینهٔ ارسال پذیرفته‌شدهٔ همین گروه.</summary>
    public decimal ShippingChargeSnapshot { get; private set; }

    /// <summary>ارز snapshot.</summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>حداقل تاریخ تحویل accepted.</summary>
    public DateOnly MinimumDeliveryDate { get; private set; }

    /// <summary>تاریخ تحویل درخواستی مشتری.</summary>
    public DateOnly? RequestedDeliveryDate { get; private set; }

    /// <summary>پنجرهٔ ساعتی درخواستی.</summary>
    public string? RequestedDeliveryTimeWindow { get; private set; }

    /// <summary>روزهای آماده‌سازی روش.</summary>
    public int LeadDays { get; private set; }

    /// <summary>وضعیت چرخهٔ عمر.</summary>
    public string State { get; private set; } = ConfirmedState;

    /// <summary>زمان snapshot.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// شاهد فشردهٔ پذیرفته‌شدهٔ همین گروه (TB-231). پس از ثبت سفارش تغییرناپذیر است؛ سفارش‌های
    /// قدیمی آن را null دارند. هیچ قیمت/تعرفهٔ فعلی این مقدار را بازمحاسبه نمی‌کند.
    /// </summary>
    public ShippingGroupAcceptedEvidence? AcceptedEvidence { get; private set; }

    /// <summary>عضویت خطوط سفارش.</summary>
    public List<OrderShippingGroupLine> Lines { get; } = [];

    internal static OrderShippingGroup Create(
        OrderShippingGroupPlan plan,
        Guid checkoutId,
        Guid sellerOrderId,
        DateTimeOffset now)
    {
        if (plan.ShippingGroupId == Guid.Empty)
        {
            throw new ContractOperationException(OrderShippingGroupFault.GroupIdentityRequired);
        }

        if (sellerOrderId == Guid.Empty)
        {
            throw new ContractOperationException(OrderShippingGroupFault.SellerOrderRequired);
        }

        if (plan.SellerPartyId == Guid.Empty)
        {
            throw new ContractOperationException(OrderShippingGroupFault.SellerRequired);
        }

        var methodCode = (plan.MethodCode ?? string.Empty).Trim();
        if (methodCode.Length == 0)
        {
            throw new ContractOperationException(OrderShippingGroupFault.MethodCodeRequired);
        }

        var currency = (plan.Currency ?? string.Empty).Trim().ToUpperInvariant();
        if (currency.Length == 0)
        {
            throw new ContractOperationException(OrderShippingGroupFault.CurrencyRequired);
        }

        if (plan.ShippingCharge < 0m)
        {
            throw new ContractOperationException(OrderShippingGroupFault.AmountInvalid);
        }

        if (!string.IsNullOrWhiteSpace(plan.OriginContext))
        {
            // No trusted fulfilment-origin authority exists yet: refuse instead of forging.
            throw new ContractOperationException(OrderShippingGroupFault.OriginUnverified);
        }

        // TB-231: شاهد فشرده اختیاری است، اما وقتی هست باید با همین گروه بخواند؛ نه clamp، نه بازمحاسبه.
        var evidence = plan.AcceptedEvidence;
        if (evidence is not null)
        {
            if (!string.Equals(evidence.Currency, currency, StringComparison.Ordinal))
            {
                throw new ContractOperationException(ShippingGroupEvidenceFault.CurrencyMismatch);
            }

            if (evidence.PayableAtCheckoutAmount != plan.ShippingCharge)
            {
                throw new ContractOperationException(ShippingGroupEvidenceFault.PayableMismatch);
            }
        }

        return new OrderShippingGroup
        {
            ShippingGroupId = plan.ShippingGroupId,
            CheckoutId = checkoutId,
            SellerOrderId = sellerOrderId,
            SellerPartyId = plan.SellerPartyId,
            MethodCode = methodCode,
            ServiceCode = Normalize(plan.ServiceCode),
            OptionCode = Normalize(plan.OptionCode),
            MethodLabel = (plan.MethodLabel ?? string.Empty).Trim(),
            OriginContext = null,
            ShippingChargeSnapshot = plan.ShippingCharge,
            Currency = currency,
            MinimumDeliveryDate = plan.MinimumDeliveryDate,
            RequestedDeliveryDate = plan.RequestedDeliveryDate,
            RequestedDeliveryTimeWindow = Normalize(plan.RequestedDeliveryTimeWindow),
            LeadDays = Math.Max(0, plan.LeadDays),
            State = ConfirmedState,
            CreatedAt = now,
            AcceptedEvidence = evidence,
        };
    }

    internal void AddLine(Guid orderLineId)
    {
        if (orderLineId == Guid.Empty)
        {
            throw new ContractOperationException(OrderShippingGroupFault.OrderLineRequired);
        }

        if (Lines.Any(x => x.OrderLineId == orderLineId))
        {
            throw new ContractOperationException(OrderShippingGroupFault.DuplicateLineMembership);
        }

        Lines.Add(OrderShippingGroupLine.Create(ShippingGroupId, orderLineId));
    }

    private static string? Normalize(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}

/// <summary>عضویت یک خط سفارش کامل در یک گروه ارسال پایدار؛ بدون تقسیم مقدار در v1.</summary>
public sealed class OrderShippingGroupLine
{
    /// <summary>سازندهٔ EF.</summary>
    private OrderShippingGroupLine()
    {
    }

    internal static OrderShippingGroupLine Create(Guid shippingGroupId, Guid orderLineId) =>
        new()
        {
            ShippingGroupId = shippingGroupId,
            OrderLineId = orderLineId,
        };

    /// <summary>گروه مالک.</summary>
    public Guid ShippingGroupId { get; init; }

    /// <summary>خط سفارش (FK مالکیت Order).</summary>
    public Guid OrderLineId { get; init; }
}

/// <summary>کدهای خطای پایدار گروه ارسال سفارش.</summary>
public static class OrderShippingGroupFault
{
    /// <summary>گروه بدون شناسه.</summary>
    public const string GroupIdentityRequired = "order.shipping.group.identity.required";

    /// <summary>گروه بدون سفارش فروشنده.</summary>
    public const string SellerOrderRequired = "order.shipping.group.seller_order.required";

    /// <summary>گروه بدون فروشنده.</summary>
    public const string SellerRequired = "order.shipping.group.seller.required";

    /// <summary>گروه بدون کد روش.</summary>
    public const string MethodCodeRequired = "order.shipping.group.method_code.required";

    /// <summary>گروه بدون ارز.</summary>
    public const string CurrencyRequired = "order.shipping.group.currency.required";

    /// <summary>مبلغ منفی.</summary>
    public const string AmountInvalid = "order.shipping.group.amount.invalid";

    /// <summary>خط سفارش نامعلوم.</summary>
    public const string OrderLineRequired = "order.shipping.group.order_line.required";

    /// <summary>مبدأ تأییدنشده.</summary>
    public const string OriginUnverified = "order.shipping.group.origin.unverified";

    /// <summary>خط در دو گروه.</summary>
    public const string DuplicateLineMembership = "order.shipping.group.line.duplicate";

    /// <summary>خط سفارش پوشش‌داده‌نشده.</summary>
    public const string MissingLineMembership = "order.shipping.group.line.missing";

    /// <summary>نقشهٔ خط سبد به خط سفارش گم است.</summary>
    public const string CartLineMappingMissing = "order.shipping.group.cart_line.mapping_missing";

    /// <summary>فروشندهٔ گروه با فروشندهٔ خط/سفارش نمی‌خواند.</summary>
    public const string SellerMismatch = "order.shipping.group.seller_mismatch";

    /// <summary>گروه بدون خط.</summary>
    public const string GroupWithoutLines = "order.shipping.group.lines.required";

    /// <summary>گروه تکراری برای همان فروشنده و روش.</summary>
    public const string DuplicateSellerMethod = "order.shipping.group.seller_method.duplicate";
}

/// <summary>
/// ورودی مادی‌سازی گروه ارسال سفارش از پیش‌نویس M1. حقیقت پول/هویت/زمان‌بندی از پیش‌نویس
/// backend-authoritative می‌آید و این نوع فقط انتقال‌دهنده است، نه منبع قیمت.
/// </summary>
public sealed record OrderShippingGroupPlan(
    Guid ShippingGroupId,
    Guid SellerPartyId,
    string MethodCode,
    string? ServiceCode,
    string? OptionCode,
    string MethodLabel,
    string? OriginContext,
    decimal ShippingCharge,
    string Currency,
    DateOnly MinimumDeliveryDate,
    DateOnly? RequestedDeliveryDate,
    string? RequestedDeliveryTimeWindow,
    int LeadDays,
    IReadOnlyList<Guid> CartLineIds,
    ShippingGroupAcceptedEvidence? AcceptedEvidence = null);
