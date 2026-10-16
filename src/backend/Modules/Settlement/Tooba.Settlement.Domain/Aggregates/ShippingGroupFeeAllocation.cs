using Tooba.BuildingBlocks;
using Tooba.Settlement.Contracts.Errors;
using Tooba.Settlement.Domain.ValueObjects;

namespace Tooba.Settlement.Domain.Aggregates;

/// <summary>
/// حساب‌داری هزینهٔ ارسال per-group مالک Settlement. کلید پایدار <see cref="ShippingGroupId"/> است و
/// هویت پرداخت/سفارش/فروشنده همراه آن نگه داشته می‌شود.
/// <para>
/// این سطر فقط charge مشتری را می‌شناسد. سهم carrier، سهم فروشنده، سهم فروشگاه، تخفیف/یارانهٔ ارسال،
/// مالیات ارسال و funder/adjustment تا زمانی که یک سیاست تجاری مجاز نساخته آن‌ها را تأمین نکند
/// <c>null</c> (= UNSPECIFIED) می‌مانند و هرگز صفر پر نمی‌شوند و residual فروشگاه فرض نمی‌شود. بنابراین
/// این سطر نه سطر merchandise سفارش فروشنده است، نه اعتبار فروشنده/فروشگاه می‌سازد و نه به payout وارد
/// می‌شود.
/// </para>
/// </summary>
public sealed class ShippingGroupFeeAllocation
{
    private ShippingGroupFeeAllocation()
    {
    }

    /// <summary>شناسهٔ سطر.</summary>
    public Guid ShippingGroupFeeId { get; init; }

    /// <summary>پرداخت مرجع.</summary>
    public Guid PaymentId { get; init; }

    /// <summary>گروه ارسال پایدار سفارش.</summary>
    public Guid ShippingGroupId { get; init; }

    /// <summary>checkout مرجع.</summary>
    public Guid CheckoutId { get; init; }

    /// <summary>سفارش فروشندهٔ مالک گروه (هویت فقط؛ سطر merchandise نیست).</summary>
    public Guid SellerOrderId { get; init; }

    /// <summary>فروشندهٔ مالک گروه (هویت فقط؛ سهم مالی نامشخص است).</summary>
    public Guid SellerPartyId { get; init; }

    /// <summary>کد متد ارسال پذیرفته‌شده.</summary>
    public string MethodCode { get; init; } = string.Empty;

    /// <summary>کد سرویس پذیرفته‌شده در صورت وجود.</summary>
    public string? ServiceCode { get; init; }

    /// <summary>کد آپشن پذیرفته‌شده در صورت وجود.</summary>
    public string? OptionCode { get; init; }

    /// <summary>نشانگر وضعیت پایدار نقل‌قول پذیرفته‌شدهٔ Order (نسخهٔ قابل‌اتکا؛ جعل نمی‌شود).</summary>
    public string AcceptedQuoteVersion { get; init; } = string.Empty;

    /// <summary>هزینهٔ ارسال مشتری در نقل‌قول پذیرفته‌شده.</summary>
    public decimal CustomerChargeAmount { get; init; }

    /// <summary>ارز charge مشتری.</summary>
    public string Currency { get; init; } = string.Empty;

    /// <summary>وضعیت تخصیص تجاری.</summary>
    public ShippingGroupFeeAllocationState AllocationState { get; init; } =
        ShippingGroupFeeAllocationState.PendingAllocation;

    /// <summary>نسخهٔ سیاست تخصیص؛ UNSPECIFIED تا زمانی که سیاست مجاز وجود ندارد.</summary>
    public string? AllocationPolicyVersion { get; init; }

    /// <summary>مرجع سیاست تخصیص؛ UNSPECIFIED تا زمانی که سیاست مجاز وجود ندارد.</summary>
    public string? AllocationPolicyReference { get; init; }

    /// <summary>مبلغ قابل‌پرداخت به carrier؛ UNSPECIFIED (بدون tariff مجاز).</summary>
    public decimal? CarrierPayableAmount { get; init; }

    /// <summary>سهم علامت‌دار فروشنده؛ UNSPECIFIED (بدون سیاست سهم مجاز).</summary>
    public decimal? SellerShareAmount { get; init; }

    /// <summary>سهم علامت‌دار marketplace؛ UNSPECIFIED (هیچ residual فرض نمی‌شود).</summary>
    public decimal? MarketplaceShareAmount { get; init; }

    /// <summary>تخفیف/یارانهٔ ارسال؛ UNSPECIFIED.</summary>
    public decimal? ShippingDiscountAmount { get; init; }

    /// <summary>مالیات ارسال؛ UNSPECIFIED.</summary>
    public decimal? ShippingTaxAmount { get; init; }

    /// <summary>تعدیل/تأمین funder؛ UNSPECIFIED.</summary>
    public decimal? FunderAdjustmentAmount { get; init; }

    /// <summary>مرجع funder در صورت وجود؛ UNSPECIFIED.</summary>
    public string? FunderReference { get; init; }

    /// <summary>نوع سطر (charge یا reversal).</summary>
    public ShippingGroupFeeRecordKind RecordKind { get; init; } = ShippingGroupFeeRecordKind.Charge;

    /// <summary>سطر charge اصلی که این سطر reversal آن است.</summary>
    public Guid? ReversalOfShippingGroupFeeId { get; init; }

    /// <summary>کلید idempotency ثبت.</summary>
    public string IdempotencyKey { get; init; } = string.Empty;

    /// <summary>زمان ثبت.</summary>
    public DateTimeOffset RecordedAt { get; init; }

    /// <summary>آیا تخصیص تجاری این سطر هنوز غیرمشخص است؟</summary>
    public bool IsPendingAllocation => AllocationState == ShippingGroupFeeAllocationState.PendingAllocation;

    /// <summary>
    /// سطر charge پذیرفته‌شدهٔ مشتری را ثبت می‌کند. هیچ مقدار سهم/مالیات/تعدیل پذیرفته نمی‌شود: همه
    /// UNSPECIFIED (= <c>null</c>) می‌مانند و وضعیت <c>PendingAllocation</c> است.
    /// </summary>
    public static ShippingGroupFeeAllocation RecordAcceptedCharge(
        Guid id,
        Guid paymentId,
        Guid shippingGroupId,
        Guid checkoutId,
        Guid sellerOrderId,
        Guid sellerPartyId,
        string methodCode,
        string? serviceCode,
        string? optionCode,
        string acceptedQuoteVersion,
        decimal customerChargeAmount,
        string currency,
        string idempotencyKey,
        DateTimeOffset now)
    {
        if (id == Guid.Empty || shippingGroupId == Guid.Empty || paymentId == Guid.Empty
            || sellerOrderId == Guid.Empty || sellerPartyId == Guid.Empty)
        {
            throw new ContractOperationException(SettlementErrorCodes.ShippingGroupIdentityRequired);
        }

        if (customerChargeAmount <= 0 || string.IsNullOrWhiteSpace(currency))
        {
            throw new ContractOperationException(SettlementErrorCodes.ShippingGroupChargeInvalid);
        }

        if (string.IsNullOrWhiteSpace(methodCode) || string.IsNullOrWhiteSpace(acceptedQuoteVersion))
        {
            throw new ContractOperationException(SettlementErrorCodes.ShippingGroupIdentityRequired);
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ContractOperationException(SettlementErrorCodes.IdempotencyRequired);
        }

        return new ShippingGroupFeeAllocation
        {
            ShippingGroupFeeId = id,
            PaymentId = paymentId,
            ShippingGroupId = shippingGroupId,
            CheckoutId = checkoutId,
            SellerOrderId = sellerOrderId,
            SellerPartyId = sellerPartyId,
            MethodCode = methodCode.Trim(),
            ServiceCode = serviceCode,
            OptionCode = optionCode,
            AcceptedQuoteVersion = acceptedQuoteVersion.Trim(),
            CustomerChargeAmount = customerChargeAmount,
            Currency = currency.Trim(),
            AllocationState = ShippingGroupFeeAllocationState.PendingAllocation,
            AllocationPolicyVersion = null,
            AllocationPolicyReference = null,
            CarrierPayableAmount = null,
            SellerShareAmount = null,
            MarketplaceShareAmount = null,
            ShippingDiscountAmount = null,
            ShippingTaxAmount = null,
            FunderAdjustmentAmount = null,
            FunderReference = null,
            RecordKind = ShippingGroupFeeRecordKind.Charge,
            ReversalOfShippingGroupFeeId = null,
            IdempotencyKey = idempotencyKey.Trim(),
            RecordedAt = now,
        };
    }

    /// <summary>
    /// سطر reversal برای یک charge پایدار می‌سازد؛ سطر اصلی دست‌نخورده می‌ماند. سطر reversal هیچ مقدار
    /// مالی حمل نمی‌کند و خودش هم PendingAllocation است.
    /// </summary>
    public static ShippingGroupFeeAllocation ReverseAcceptedCharge(
        Guid id,
        ShippingGroupFeeAllocation original,
        string idempotencyKey,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(original);
        if (original.RecordKind != ShippingGroupFeeRecordKind.Charge)
        {
            throw new ContractOperationException(SettlementErrorCodes.ShippingGroupIdentityRequired);
        }

        if (id == Guid.Empty)
        {
            throw new ContractOperationException(SettlementErrorCodes.ShippingGroupIdentityRequired);
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ContractOperationException(SettlementErrorCodes.IdempotencyRequired);
        }

        return new ShippingGroupFeeAllocation
        {
            ShippingGroupFeeId = id,
            PaymentId = original.PaymentId,
            ShippingGroupId = original.ShippingGroupId,
            CheckoutId = original.CheckoutId,
            SellerOrderId = original.SellerOrderId,
            SellerPartyId = original.SellerPartyId,
            MethodCode = original.MethodCode,
            ServiceCode = original.ServiceCode,
            OptionCode = original.OptionCode,
            AcceptedQuoteVersion = original.AcceptedQuoteVersion,
            CustomerChargeAmount = original.CustomerChargeAmount,
            Currency = original.Currency,
            AllocationState = ShippingGroupFeeAllocationState.PendingAllocation,
            RecordKind = ShippingGroupFeeRecordKind.Reversal,
            ReversalOfShippingGroupFeeId = original.ShippingGroupFeeId,
            IdempotencyKey = idempotencyKey.Trim(),
            RecordedAt = now,
        };
    }

    /// <summary>
    /// نگهبان fail-closed: سطر گروه ارسال هرگز سطر merchandise سفارش فروشنده یا اعتبار فروشنده/فروشگاه
    /// نیست. هیچ ورودی‌ای این تبدیل را مجاز نمی‌کند، زیرا هیچ سهم مجازی مجاز نشده است.
    /// </summary>
    public void EnsureNotSellerRevenue() =>
        throw new ContractOperationException(SettlementErrorCodes.ShippingGroupNotSellerRevenue);

    /// <summary>
    /// نگهبان fail-closed: تا وقتی سهم carrier/فروشنده/فروشگاه/مالیات UNSPECIFIED است، این سطر نمی‌تواند
    /// settlement یا payout شود.
    /// </summary>
    public void EnsurePayoutAllocationAvailable()
    {
        if (IsPendingAllocation)
        {
            throw new ContractOperationException(SettlementErrorCodes.ShippingGroupAllocationPolicyMissing);
        }
    }
}
