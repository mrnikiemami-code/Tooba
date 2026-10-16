using Tooba.BuildingBlocks;

namespace Tooba.Order.Domain.Checkout;

/// <summary>
/// Membership of one whole cart line in one cart shipping group. V1 assigns WHOLE lines only;
/// partial-quantity splitting is deliberately not modelled here.
/// Any transport must classify this invariant violation as <see cref="CartShippingGroupFault"/>.
/// </summary>
public sealed class CartShippingDraftGroupLine
{
    /// <summary>سازندهٔ EF.</summary>
    private CartShippingDraftGroupLine()
    {
    }

    /// <summary>گروه مالک.</summary>
    public Guid ShippingGroupId { get; init; }

    /// <summary>شناسهٔ خط سبد (مالکیت Cart؛ بدون FK بین‌ماژولی).</summary>
    public Guid CartLineId { get; init; }

    internal static CartShippingDraftGroupLine Create(Guid shippingGroupId, Guid cartLineId)
    {
        if (shippingGroupId == Guid.Empty)
        {
            throw new ContractOperationException(CartShippingGroupFault.GroupIdentityRequired);
        }

        if (cartLineId == Guid.Empty)
        {
            throw new ContractOperationException(CartShippingGroupFault.LineIdentityRequired);
        }

        return new CartShippingDraftGroupLine
        {
            ShippingGroupId = shippingGroupId,
            CartLineId = cartLineId,
        };
    }
}

/// <summary>
/// Stable fault codes for the cart shipping-group draft. Order-owned and stable across transports;
/// the Application layer maps them to its own localized storefront error surface.
/// </summary>
public static class CartShippingGroupFault
{
    /// <summary>گروه بدون شناسه.</summary>
    public const string GroupIdentityRequired = "shipping.group.identity.required";

    /// <summary>گروه بدون سبد مالک.</summary>
    public const string CartRequired = "shipping.group.cart.required";

    /// <summary>خط بدون شناسه.</summary>
    public const string LineIdentityRequired = "shipping.group.line.identity.required";

    /// <summary>گروه بدون فروشنده.</summary>
    public const string SellerRequired = "shipping.group.seller.required";

    /// <summary>گروه بدون کد روش متعارف.</summary>
    public const string MethodCodeRequired = "shipping.group.method_code.required";

    /// <summary>گروه بدون ارز.</summary>
    public const string CurrencyRequired = "shipping.group.currency.required";

    /// <summary>گروه بدون حداقل تاریخ تحویل معنادار.</summary>
    public const string MinimumDeliveryDateInvalid = "shipping.group.minimum_delivery_date.invalid";

    /// <summary>مبلغ منفی.</summary>
    public const string AmountInvalid = "shipping.group.amount.invalid";

    /// <summary>خطوط یک گروه به بیش از یک فروشنده تعلق دارند (ادغام نامعتبر).</summary>
    public const string MixedSellerLines = "shipping.group.mixed_seller_lines";

    /// <summary>گروه بدون خط.</summary>
    public const string GroupWithoutLines = "shipping.group.lines.required";

    /// <summary>یک خط در دو گروه.</summary>
    public const string DuplicateLineMembership = "shipping.group.line.duplicate";

    /// <summary>خط سبدِ ناشناس/بیگانه.</summary>
    public const string UnknownLine = "shipping.group.line.unknown";

    /// <summary>خط سبد جاافتاده (همهٔ خطوط باید پوشش داده شوند).</summary>
    public const string MissingLineMembership = "shipping.group.line.missing";

    /// <summary>روش انتخاب‌شده در فهرست واجد شرایط authoritative نیست.</summary>
    public const string MethodNotEligible = "shipping.group.method.not_eligible";

    /// <summary>کد سرویس/گزینهٔ اعلامی با هویت متعارف روش نمی‌خواند.</summary>
    public const string MethodIdentityMismatch = "shipping.group.method.identity_mismatch";

    /// <summary>مبدأ fulfilment نامعلوم/غیرقابل‌اعتماد؛ ادغام ناایمن رد می‌شود.</summary>
    public const string OriginUnverified = "shipping.group.origin.unverified";

    /// <summary>گروه تکراری برای همان فروشنده و روش.</summary>
    public const string DuplicateSellerMethod = "shipping.group.seller_method.duplicate";
}

/// <summary>
/// Pre-order (cart-scoped) shipping group owned by Order. One group belongs to exactly one seller
/// and carries ONE canonical shipping method. It is a draft: it never creates an order, reservation
/// or checkout, and it never trusts client-supplied money — amount/currency/label come from the
/// authoritative eligible method.
/// </summary>
public sealed class CartShippingDraftGroup
{
    /// <summary>حداکثر طول کد روش/سرویس/گزینه.</summary>
    public const int MaxCodeLength = 64;

    /// <summary>حداکثر طول برچسب روش.</summary>
    public const int MaxLabelLength = 128;

    /// <summary>حداکثر طول بافت مبدأ.</summary>
    public const int MaxOriginLength = 64;

    /// <summary>وضعیت پایدار پیش‌نویس.</summary>
    public const string DraftState = "Draft";

    /// <summary>سازندهٔ EF.</summary>
    private CartShippingDraftGroup()
    {
    }

    /// <summary>شناسهٔ پایدار گروه.</summary>
    public Guid ShippingGroupId { get; init; }

    /// <summary>سبد مالک.</summary>
    public Guid CartId { get; init; }

    /// <summary>فروشندهٔ مالک گروه (از Cart، نه از کلاینت).</summary>
    public Guid SellerPartyId { get; private set; }

    /// <summary>کد روش متعارف (مثل tipax:standard).</summary>
    public string MethodCode { get; private set; } = string.Empty;

    /// <summary>کد سرویس؛ فیلد جداگانه و هرگز بخشی از برچسب نیست.</summary>
    public string? ServiceCode { get; private set; }

    /// <summary>کد گزینه؛ فیلد جداگانه.</summary>
    public string? OptionCode { get; private set; }

    /// <summary>برچسب نمایشی؛ فقط ارائه، هرگز حقیقت تجاری.</summary>
    public string MethodLabel { get; private set; } = string.Empty;

    /// <summary>
    /// بافت مبدأ fulfilment. در این اسلایس هیچ مرجع مورداعتمادی برای مبدأ در انتخاب ارسال وجود
    /// ندارد، پس فقط null مجاز است و مقدار اعلامی رد می‌شود (بدون جعل انبار).
    /// </summary>
    public string? OriginContext { get; private set; }

    /// <summary>مبلغ ارسال نقل‌قول‌شدهٔ authoritative بک‌اند.</summary>
    public decimal ShippingAmount { get; private set; }

    /// <summary>ارز نقل‌قول.</summary>
    public string Currency { get; private set; } = string.Empty;

    /// <summary>حداقل تاریخ تحویل محاسبه‌شده برای همین گروه.</summary>
    public DateOnly MinimumDeliveryDate { get; private set; }

    /// <summary>تاریخ تحویل انتخابی مشتری.</summary>
    public DateOnly? SelectedDeliveryDate { get; private set; }

    /// <summary>پنجرهٔ ساعتی انتخابی.</summary>
    public string? SelectedDeliveryTimeWindow { get; private set; }

    /// <summary>روزهای آماده‌سازی مؤثر روش.</summary>
    public int LeadDays { get; private set; }

    /// <summary>وضعیت چرخهٔ عمر گروه در سطح پیش‌نویس.</summary>
    public string State { get; private set; } = DraftState;

    /// <summary>زمان به‌روزرسانی.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>اعضای خط سبد.</summary>
    public List<CartShippingDraftGroupLine> Lines { get; } = [];

    /// <summary>گروه پیش‌نویس با هویت متعارف و مبلغ authoritative می‌سازد؛ مبلغ از کلاینت گرفته نمی‌شود.</summary>
    internal static CartShippingDraftGroup Create(
        Guid shippingGroupId,
        Guid cartId,
        Guid sellerPartyId,
        string methodCode,
        string? serviceCode,
        string? optionCode,
        string methodLabel,
        string? originContext,
        decimal shippingAmount,
        string currency,
        DateOnly minimumDeliveryDate,
        DateOnly? selectedDeliveryDate,
        string? selectedDeliveryTimeWindow,
        int leadDays,
        DateTimeOffset now)
    {
        if (shippingGroupId == Guid.Empty)
        {
            throw new ContractOperationException(CartShippingGroupFault.GroupIdentityRequired);
        }

        if (cartId == Guid.Empty)
        {
            throw new ContractOperationException(CartShippingGroupFault.CartRequired);
        }

        if (sellerPartyId == Guid.Empty)
        {
            throw new ContractOperationException(CartShippingGroupFault.SellerRequired);
        }

        var code = Normalize(methodCode, MaxCodeLength);
        if (code.Length == 0)
        {
            throw new ContractOperationException(CartShippingGroupFault.MethodCodeRequired);
        }

        var normalizedCurrency = (currency ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedCurrency.Length == 0)
        {
            throw new ContractOperationException(CartShippingGroupFault.CurrencyRequired);
        }

        if (shippingAmount < 0m)
        {
            throw new ContractOperationException(CartShippingGroupFault.AmountInvalid);
        }

        if (!string.IsNullOrWhiteSpace(originContext))
        {
            // No trusted fulfilment-origin authority exists at draft-selection time: refuse instead of forging.
            throw new ContractOperationException(CartShippingGroupFault.OriginUnverified);
        }

        return new CartShippingDraftGroup
        {
            ShippingGroupId = shippingGroupId,
            CartId = cartId,
            SellerPartyId = sellerPartyId,
            MethodCode = code,
            ServiceCode = NormalizeOptional(serviceCode, MaxCodeLength),
            OptionCode = NormalizeOptional(optionCode, MaxCodeLength),
            MethodLabel = Normalize(methodLabel, MaxLabelLength),
            OriginContext = null,
            ShippingAmount = shippingAmount,
            Currency = normalizedCurrency,
            MinimumDeliveryDate = minimumDeliveryDate,
            SelectedDeliveryDate = selectedDeliveryDate,
            SelectedDeliveryTimeWindow = string.IsNullOrWhiteSpace(selectedDeliveryTimeWindow)
                ? null
                : selectedDeliveryTimeWindow.Trim(),
            LeadDays = Math.Max(0, leadDays),
            State = DraftState,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// عضو کل خط را اضافه می‌کند. تمام-خط است؛ مقدار جزئی در این اسلایس مدل نمی‌شود.
    /// فروشندهٔ خط باید همان فروشندهٔ گروه باشد (ادغام دو فروشنده ممنوع).
    /// </summary>
    internal void AddWholeLine(Guid cartLineId, Guid lineSellerPartyId)
    {
        if (lineSellerPartyId != SellerPartyId)
        {
            throw new ContractOperationException(CartShippingGroupFault.MixedSellerLines);
        }

        if (Lines.Any(x => x.CartLineId == cartLineId))
        {
            throw new ContractOperationException(CartShippingGroupFault.DuplicateLineMembership);
        }

        Lines.Add(CartShippingDraftGroupLine.Create(ShippingGroupId, cartLineId));
    }

    /// <summary>پس از ساخت کامل، گروه بدون خط نامعتبر است.</summary>
    internal void EnsureHasLines()
    {
        if (Lines.Count == 0)
        {
            throw new ContractOperationException(CartShippingGroupFault.GroupWithoutLines);
        }
    }

    /// <summary>چانه‌زنی تاریخ تحویل انتخابی را روی همین گروه جایگزین می‌کند.</summary>
    internal void ApplySchedule(DateOnly? selectedDeliveryDate, string? selectedDeliveryTimeWindow)
    {
        SelectedDeliveryDate = selectedDeliveryDate;
        SelectedDeliveryTimeWindow = string.IsNullOrWhiteSpace(selectedDeliveryTimeWindow)
            ? null
            : selectedDeliveryTimeWindow.Trim();
    }

    private static string Normalize(string value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ContractOperationException(CartShippingGroupFault.MethodCodeRequired);
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.Length > maxLength)
        {
            throw new ContractOperationException(CartShippingGroupFault.MethodCodeRequired);
        }

        return trimmed;
    }
}
