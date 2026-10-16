using Tooba.BuildingBlocks;

namespace Tooba.Fulfillment.Domain.Aggregates;

/// <summary>
/// گروه ارسال پذیرفته‌شدهٔ مالکیت Fulfillment. هویت پایدار <see cref="ShippingGroupId"/> از
/// سفارش پذیرفته‌شده می‌آید (نه برچسب، نه مقدار مرورگر) و هیچ نرخ/تخصیص carrier در اینجا ساخته نمی‌شود.
/// عضویت خطوط روی <see cref="FulfillmentItem.ShippingGroupId"/> نگه داشته می‌شود تا یک خط
/// دقیقاً به یک گروه تعلق داشته باشد.
/// </summary>
public sealed class FulfillmentShippingGroup
{
    private FulfillmentShippingGroup()
    {
    }

    /// <summary>fulfillment مالک (یک واحد برای یک SellerOrder).</summary>
    public Guid FulfillmentId { get; init; }

    /// <summary>هویت پایدار گروه از سفارش پذیرفته‌شده.</summary>
    public Guid ShippingGroupId { get; init; }

    /// <summary>سفارش فروشندهٔ مالک؛ باید با واحد fulfillment یکی باشد.</summary>
    public Guid SellerOrderId { get; init; }

    /// <summary>فروشندهٔ گروه.</summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>کد روش متعارف پذیرفته‌شده.</summary>
    public string MethodCode { get; init; } = string.Empty;

    /// <summary>کد سرویس همان روش.</summary>
    public string? ServiceCode { get; init; }

    /// <summary>کد گزینه همان روش.</summary>
    public string? OptionCode { get; init; }

    /// <summary>برچسب snapshot برای ارائه؛ مرجع تصمیم نیست.</summary>
    public string MethodLabel { get; init; } = string.Empty;

    /// <summary>بافت مبدأ؛ تا نبود مرجع مورداعتماد همیشه null است و جعل نمی‌شود.</summary>
    public string? OriginContext { get; init; }

    /// <summary>حداقل تاریخ تحویل پذیرفته‌شده.</summary>
    public DateOnly MinimumDeliveryDate { get; init; }

    /// <summary>تاریخ تحویل درخواستی مشتری.</summary>
    public DateOnly? RequestedDeliveryDate { get; init; }

    /// <summary>پنجرهٔ ساعتی درخواستی.</summary>
    public string? RequestedDeliveryTimeWindow { get; init; }

    /// <summary>روزهای آماده‌سازی روش.</summary>
    public int LeadDays { get; init; }

    internal static FulfillmentShippingGroup Create(
        FulfillmentShippingGroupPlan plan,
        Guid fulfillmentId,
        Guid sellerOrderId,
        Guid sellerPartyId)
    {
        if (plan.ShippingGroupId == Guid.Empty)
        {
            throw new ContractOperationException(FulfillmentShippingGroupFault.IdentityRequired);
        }

        if (plan.SellerPartyId == Guid.Empty)
        {
            throw new ContractOperationException(FulfillmentShippingGroupFault.SellerRequired);
        }

        if (plan.SellerPartyId != sellerPartyId)
        {
            throw new ContractOperationException(FulfillmentShippingGroupFault.SellerMismatch);
        }

        var methodCode = (plan.MethodCode ?? string.Empty).Trim();
        if (methodCode.Length == 0)
        {
            throw new ContractOperationException(FulfillmentShippingGroupFault.MethodCodeRequired);
        }

        if (plan.OrderLineIds is null || plan.OrderLineIds.Count == 0)
        {
            throw new ContractOperationException(FulfillmentShippingGroupFault.LinesRequired);
        }

        if (plan.OrderLineIds.Any(x => x == Guid.Empty))
        {
            throw new ContractOperationException(FulfillmentShippingGroupFault.OrderLineRequired);
        }

        if (plan.OrderLineIds.Distinct().Count() != plan.OrderLineIds.Count)
        {
            throw new ContractOperationException(FulfillmentShippingGroupFault.LineDuplicate);
        }

        return new FulfillmentShippingGroup
        {
            FulfillmentId = fulfillmentId,
            ShippingGroupId = plan.ShippingGroupId,
            SellerOrderId = sellerOrderId,
            SellerPartyId = sellerPartyId,
            MethodCode = methodCode,
            ServiceCode = Normalize(plan.ServiceCode),
            OptionCode = Normalize(plan.OptionCode),
            MethodLabel = (plan.MethodLabel ?? string.Empty).Trim(),
            OriginContext = string.IsNullOrWhiteSpace(plan.OriginContext) ? null : plan.OriginContext.Trim(),
            MinimumDeliveryDate = plan.MinimumDeliveryDate,
            RequestedDeliveryDate = plan.RequestedDeliveryDate,
            RequestedDeliveryTimeWindow = Normalize(plan.RequestedDeliveryTimeWindow),
            LeadDays = Math.Max(0, plan.LeadDays),
        };
    }

    private static string? Normalize(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}

/// <summary>
/// انتقال‌دهندهٔ گروه ارسال از snapshot سفارش پذیرفته‌شده به Fulfillment.
/// منبع قیمت/هویت نیست؛ فقط انتقال snapshot است.
/// </summary>
public sealed record FulfillmentShippingGroupPlan(
    Guid ShippingGroupId,
    Guid SellerPartyId,
    string MethodCode,
    string? ServiceCode,
    string? OptionCode,
    string MethodLabel,
    string? OriginContext,
    DateOnly MinimumDeliveryDate,
    DateOnly? RequestedDeliveryDate,
    string? RequestedDeliveryTimeWindow,
    int LeadDays,
    IReadOnlyList<Guid> OrderLineIds);

/// <summary>کدهای خطای پایدار گروه ارسال Fulfillment.</summary>
public static class FulfillmentShippingGroupFault
{
    /// <summary>گروه بدون شناسه.</summary>
    public const string IdentityRequired = "fulfillment.shipping_group.identity_required";

    /// <summary>گروه بدون فروشنده.</summary>
    public const string SellerRequired = "fulfillment.shipping_group.seller_required";

    /// <summary>فروشندهٔ گروه با فروشندهٔ SellerOrder نمی‌خواند.</summary>
    public const string SellerMismatch = "fulfillment.shipping_group.seller_mismatch";

    /// <summary>گروه بدون کد روش.</summary>
    public const string MethodCodeRequired = "fulfillment.shipping_group.method_code_required";

    /// <summary>گروه بدون خط.</summary>
    public const string LinesRequired = "fulfillment.shipping_group.lines_required";

    /// <summary>خط نامعلوم در گروه.</summary>
    public const string OrderLineRequired = "fulfillment.shipping_group.order_line_required";

    /// <summary>خط تکراری داخل یک گروه.</summary>
    public const string LineDuplicate = "fulfillment.shipping_group.line_duplicate";

    /// <summary>شناسهٔ گروه تکراری در یک واحد.</summary>
    public const string GroupDuplicate = "fulfillment.shipping_group.duplicate";

    /// <summary>خطی در دو گروه.</summary>
    public const string LineInMultipleGroups = "fulfillment.shipping_group.line_multiple";

    /// <summary>خط سفارش بدون گروه (پوشش ناقص).</summary>
    public const string LineUncovered = "fulfillment.shipping_group.line_uncovered";

    /// <summary>خط گروه در سفارش این واحد وجود ندارد.</summary>
    public const string LineUnknown = "fulfillment.shipping_group.line_unknown";

    /// <summary>سفارش گروه‌دار بدون گروه برای محموله.</summary>
    public const string ShipmentGroupRequired = "fulfillment.shipment.shipping_group_required";

    /// <summary>گروه اعلام‌شده در این واحد وجود ندارد.</summary>
    public const string GroupNotFound = "fulfillment.shipment.shipping_group_not_found";

    /// <summary>خطوط محموله به گروه‌های متفاوت تعلق دارند.</summary>
    public const string ShipmentGroupMixed = "fulfillment.shipment.shipping_group_mixed";

    /// <summary>گروه ارسال روی سفارش بدون گروه‌بندی اعلام شده است.</summary>
    public const string GroupNotApplicable = "fulfillment.shipment.shipping_group_not_applicable";
}
