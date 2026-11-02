using Tooba.BuildingBlocks;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;

namespace Tooba.Shipping.Domain.Carriers;

/// <summary>Durable carrier shipment operation. Financial Order evidence is never stored here.</summary>
public sealed class ShippingCarrierShipmentOperation
{
    /// <summary>حداکثر طول کلید پایدار.</summary>
    public const int MaxKeyLength = 128;

    /// <summary>حداکثر طول کد provider.</summary>
    public const int MaxProviderCodeLength = 64;

    /// <summary>حداکثر طول کد رهگیری.</summary>
    public const int MaxTrackingLength = 64;

    /// <summary>حداکثر طول وضعیت خام provider.</summary>
    public const int MaxStatusLength = 128;

    /// <summary>حداکثر طول کد خطا.</summary>
    public const int MaxErrorLength = 128;

    private ShippingCarrierShipmentOperation()
    {
    }

    /// <summary>شناسهٔ پایدار عملیات.</summary>
    public Guid OperationId { get; private set; }

    /// <summary>کلید پایدار برای replay همزمان.</summary>
    public string OperationKey { get; private set; } = string.Empty;

    /// <summary>گونهٔ عملیات.</summary>
    public ShippingCarrierShipmentKind Kind { get; private set; }

    /// <summary>وضعیت پایدار.</summary>
    public ShippingCarrierShipmentState State { get; private set; }

    /// <summary>گونهٔ مالک.</summary>
    public ShippingDraftOwnerKind OwnerKind { get; private set; }

    /// <summary>tenant مالک Store یا تهی.</summary>
    public string? OwnerTenantId { get; private set; }

    /// <summary>فروشندهٔ مالک Seller یا تهی.</summary>
    public Guid? OwnerSellerPartyId { get; private set; }

    /// <summary>سفارش متعهد.</summary>
    public Guid CheckoutId { get; private set; }

    /// <summary>گروه ارسال متعهد.</summary>
    public Guid ShippingGroupId { get; private set; }

    /// <summary>کد provider خنثی.</summary>
    public string ProviderCode { get; private set; } = string.Empty;

    /// <summary>حساب حامل.</summary>
    public Guid AccountId { get; private set; }

    /// <summary>نسخهٔ پیکربندی حساب.</summary>
    public int ConfigurationVersion { get; private set; }

    /// <summary>کد رهگیری واقعی provider یا تهی.</summary>
    public string? ProviderTrackingCode { get; private set; }

    /// <summary>شناسهٔ سفارش provider یا تهی.</summary>
    public string? ProviderOrderId { get; private set; }

    /// <summary>وضعیت خام provider یا تهی.</summary>
    public string? ProviderStatus { get; private set; }

    /// <summary>آخرین کد خطای پایدار یا تهی.</summary>
    public string? LastErrorCode { get; private set; }

    /// <summary>زمان تلاش.</summary>
    public DateTimeOffset AttemptedAt { get; private set; }

    /// <summary>زمان اتمام یا تهی.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Durable sender/package input persisted before outbound provider create.</summary>
    public string? ExecutionInputSnapshotJson { get; private set; }

    /// <summary>عملیات pending را با کلید پایدار می‌سازد.</summary>
    public static ShippingCarrierShipmentOperation Begin(
        Guid operationId,
        string operationKey,
        ShippingCarrierShipmentKind kind,
        ShippingDraftOwnerKind ownerKind,
        string? ownerTenantId,
        Guid? ownerSellerPartyId,
        Guid checkoutId,
        Guid shippingGroupId,
        string providerCode,
        Guid accountId,
        int configurationVersion,
        DateTimeOffset now)
    {
        var key = (operationKey ?? string.Empty).Trim();
        var provider = (providerCode ?? string.Empty).Trim();
        if (operationId == Guid.Empty
            || key.Length is 0 or > MaxKeyLength
            || checkoutId == Guid.Empty
            || shippingGroupId == Guid.Empty
            || accountId == Guid.Empty
            || configurationVersion < 1
            || provider.Length is 0 or > MaxProviderCodeLength)
        {
            throw new ContractOperationException(ShippingErrorCodes.CarrierShipmentRequestInvalid);
        }

        return new ShippingCarrierShipmentOperation
        {
            OperationId = operationId,
            OperationKey = key,
            Kind = kind,
            State = ShippingCarrierShipmentState.Pending,
            OwnerKind = ownerKind,
            OwnerTenantId = string.IsNullOrWhiteSpace(ownerTenantId) ? null : ownerTenantId.Trim(),
            OwnerSellerPartyId = ownerSellerPartyId is Guid id && id != Guid.Empty ? id : null,
            CheckoutId = checkoutId,
            ShippingGroupId = shippingGroupId,
            ProviderCode = provider,
            AccountId = accountId,
            ConfigurationVersion = configurationVersion,
            AttemptedAt = now,
        };
    }

    /// <summary>Persists execution input before any outbound provider call.</summary>
    public void SetExecutionInputSnapshot(string snapshotJson)
    {
        var trimmed = (snapshotJson ?? string.Empty).Trim();
        if (trimmed.Length == 0)
        {
            throw new ContractOperationException(ShippingErrorCodes.CarrierShipmentRequestInvalid);
        }

        ExecutionInputSnapshotJson = trimmed.Length > 8000 ? trimmed[..8000] : trimmed;
    }

    /// <summary>پاسخ موفق provider را پایدار می‌کند.</summary>
    public void Succeed(
        DateTimeOffset now,
        string? trackingCode,
        string? providerOrderId,
        string? providerStatus)
    {
        EnsurePending();
        ProviderTrackingCode = NormalizeOptional(trackingCode, MaxTrackingLength);
        ProviderOrderId = NormalizeOptional(providerOrderId, MaxTrackingLength);
        ProviderStatus = NormalizeOptional(providerStatus, MaxStatusLength);
        LastErrorCode = null;
        State = ShippingCarrierShipmentState.Succeeded;
        CompletedAt = now;
    }

    /// <summary>شکست قطعی را پایدار می‌کند.</summary>
    public void Fail(DateTimeOffset now, string errorCode)
    {
        EnsurePending();
        LastErrorCode = NormalizeRequired(errorCode, MaxErrorLength);
        State = ShippingCarrierShipmentState.Failed;
        CompletedAt = now;
    }

    /// <summary>نتیجهٔ مبهم را پایدار می‌کند تا بدون تصمیم اپراتور تکرار نشود.</summary>
    public void MarkUnknown(DateTimeOffset now, string errorCode)
    {
        EnsurePending();
        LastErrorCode = NormalizeRequired(errorCode, MaxErrorLength);
        State = ShippingCarrierShipmentState.Unknown;
        CompletedAt = now;
    }

    /// <summary>وضعیت رهگیری provider را روی عملیات موفق/مبهم می‌نویسد.</summary>
    public void ApplyTrack(DateTimeOffset now, string? trackingCode, string? providerStatus)
    {
        if (State is not (ShippingCarrierShipmentState.Succeeded or ShippingCarrierShipmentState.Unknown))
        {
            throw new ContractOperationException(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        if (!string.IsNullOrWhiteSpace(trackingCode))
        {
            ProviderTrackingCode = NormalizeOptional(trackingCode, MaxTrackingLength);
        }

        ProviderStatus = string.IsNullOrWhiteSpace(providerStatus)
            ? "unknown"
            : NormalizeOptional(providerStatus, MaxStatusLength);
        AttemptedAt = now;
    }

    /// <summary>لغو موفق را روی محمولهٔ موفق با کد رهگیری می‌نویسد.</summary>
    public void ApplyCancel(DateTimeOffset now, string? providerStatus)
    {
        if (State != ShippingCarrierShipmentState.Succeeded
            || string.IsNullOrWhiteSpace(ProviderTrackingCode))
        {
            throw new ContractOperationException(ShippingErrorCodes.CarrierShipmentNotEligible);
        }

        ProviderStatus = string.IsNullOrWhiteSpace(providerStatus)
            ? "cancelled"
            : NormalizeOptional(providerStatus, MaxStatusLength);
        AttemptedAt = now;
        CompletedAt = now;
    }

    private void EnsurePending()
    {
        if (State != ShippingCarrierShipmentState.Pending)
        {
            throw new ContractOperationException(ShippingErrorCodes.CarrierShipmentNotEligible);
        }
    }

    private static string NormalizeRequired(string value, int max)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0 || trimmed.Length > max)
        {
            throw new ContractOperationException(ShippingErrorCodes.CarrierShipmentRequestInvalid);
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > max ? trimmed[..max] : trimmed;
    }
}

/// <summary>گونهٔ عملیات حاملی پایدار.</summary>
public enum ShippingCarrierShipmentKind
{
    /// <summary>ایجاد محموله.</summary>
    Create = 0,

    /// <summary>رهگیری.</summary>
    Track = 1,

    /// <summary>لغو.</summary>
    Cancel = 2
}

/// <summary>وضعیت پایدار عملیات حاملی.</summary>
public enum ShippingCarrierShipmentState
{
    /// <summary>در انتظار پاسخ provider.</summary>
    Pending = 0,

    /// <summary>موفق.</summary>
    Succeeded = 1,

    /// <summary>شکست قطعی.</summary>
    Failed = 2,

    /// <summary>مبهم؛ بدون تصمیم اپراتور تکرار نمی‌شود.</summary>
    Unknown = 3
}
