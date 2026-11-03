namespace Tooba.Shipping.Contracts.Carriers;

/// <summary>وضعیت پایدار عملیات حاملی؛ جدا از شاهد مالی Order.</summary>
public enum ShippingCarrierShipmentStateView
{
    /// <summary>ثبت شده و در انتظار پاسخ provider.</summary>
    Pending = 0,

    /// <summary>provider پاسخ موفق داده است.</summary>
    Succeeded = 1,

    /// <summary>provider رد کرده یا درخواست نامعتبر بوده است.</summary>
    Failed = 2,

    /// <summary>نتیجه مبهم است و بدون تصمیم اپراتور تکرار نمی‌شود.</summary>
    Unknown = 3
}

/// <summary>گونهٔ عملیات حاملی پایدار.</summary>
public enum ShippingCarrierShipmentKindView
{
    /// <summary>ایجاد محموله.</summary>
    Create = 0,

    /// <summary>رهگیری محموله.</summary>
    Track = 1,

    /// <summary>لغو محموله.</summary>
    Cancel = 2
}

/// <summary>نمایهٔ عملیات حاملی بدون راز و بدون پول سفارش.</summary>
/// <param name="OperationId">شناسهٔ پایدار عملیات.</param>
/// <param name="OperationKey">کلید پایدار برای replay.</param>
/// <param name="Kind">گونهٔ عملیات.</param>
/// <param name="State">وضعیت پایدار.</param>
/// <param name="CheckoutId">سفارش متعهد.</param>
/// <param name="ShippingGroupId">گروه ارسال متعهد.</param>
/// <param name="ProviderCode">کد provider خنثی.</param>
/// <param name="AccountId">حساب حامل مالک.</param>
/// <param name="ConfigurationVersion">نسخهٔ پیکربندی حساب در زمان عملیات.</param>
/// <param name="ProviderTrackingCode">کد رهگیری صادرشده یا تهی.</param>
/// <param name="ProviderOrderId">شناسهٔ سفارش provider یا تهی.</param>
/// <param name="ProviderStatus">وضعیت خام provider یا تهی.</param>
/// <param name="LastErrorCode">آخرین کد خطای پایدار یا تهی.</param>
/// <param name="CanCreate">آیا adapter ایجاد را پشتیبانی می‌کند.</param>
/// <param name="CanTrack">آیا adapter رهگیری را پشتیبانی می‌کند.</param>
/// <param name="CanCancel">آیا adapter لغو را پشتیبانی می‌کند.</param>
/// <param name="CanLabel">آیا adapter برچسب را پشتیبانی می‌کند.</param>
/// <param name="AttemptedAt">زمان تلاش.</param>
/// <param name="CompletedAt">زمان اتمام یا تهی.</param>
public sealed record ShippingCarrierShipmentOperationView(
    Guid OperationId,
    string OperationKey,
    ShippingCarrierShipmentKindView Kind,
    ShippingCarrierShipmentStateView State,
    Guid CheckoutId,
    Guid ShippingGroupId,
    string ProviderCode,
    Guid AccountId,
    int ConfigurationVersion,
    string? ProviderTrackingCode,
    string? ProviderOrderId,
    string? ProviderStatus,
    string? LastErrorCode,
    bool CanCreate,
    bool CanTrack,
    bool CanCancel,
    bool CanLabel,
    DateTimeOffset AttemptedAt,
    DateTimeOffset? CompletedAt);

/// <summary>طرف فرستنده یا گیرنده برای ایجاد محموله.</summary>
/// <param name="FullName">نام کامل.</param>
/// <param name="Mobile">شمارهٔ تماس.</param>
/// <param name="AddressLine">نشانی.</param>
/// <param name="LocationCode">کد شهر provider.</param>
/// <param name="PostalCode">کدپستی یا تهی.</param>
public sealed record ShippingCarrierShipmentPartyInput(
    string FullName,
    string Mobile,
    string AddressLine,
    string LocationCode,
    string? PostalCode = null);

/// <summary>Configured sender hints from account/method binding (nullable until complete).</summary>
public sealed record ShippingCarrierConfiguredSenderView(
    string? FullName,
    string? Mobile,
    string? AddressLine,
    string? PostalCode,
    string? LocationCode);

/// <summary>Latest durable create operation summary for one group.</summary>
public sealed record ShippingCarrierLatestCreateOperationView(
    Guid? OperationId,
    ShippingCarrierShipmentStateView? State,
    string? ProviderTrackingCode);

/// <summary>Server-authoritative carrier execution capability for one committed shipping group.</summary>
public sealed record ShippingGroupCarrierExecutionCapabilityView(
    Guid CheckoutId,
    Guid ShippingGroupId,
    Guid SellerPartyId,
    Guid SellerOrderId,
    string MethodCode,
    string MethodLabel,
    string TransportMode,
    int OwnerKind,
    string? OwnerTenantKey,
    Guid? OwnerSellerPartyId,
    string RecipientName,
    string RecipientMobile,
    string RecipientAddress,
    string? PostalCode,
    string ProvinceName,
    string CityName,
    string Currency,
    decimal PayableAtCheckoutAmount,
    decimal CarrierCollectAmount,
    bool HasCarrierQuote,
    Guid? CarrierAccountId,
    int? ConfigurationVersion,
    string? ProviderCode,
    string? ProviderServiceCode,
    string? QuoteInputIdentity,
    string? OriginLocationCode,
    string? DestinationLocationCode,
    decimal? WeightGrams,
    decimal? DeclaredValue,
    decimal? LengthCentimeters,
    decimal? WidthCentimeters,
    decimal? HeightCentimeters,
    string SellerOrderStatus,
    bool IsCancelled,
    bool PaymentLocked,
    bool CanExecuteCarrierShipment,
    string? IneligibilityReasonCode,
    IReadOnlyList<Guid> OrderLineIds,
    bool CanCreate,
    bool CanTrack,
    bool CanCancel,
    bool CanLabel,
    IReadOnlyList<string> BlockReasonCodes,
    ShippingCarrierConfiguredSenderView ConfiguredSender,
    bool PackageReady,
    bool AllowPackageCompletion,
    bool HasEditableSenderPostal,
    bool HasEditableSenderContact,
    ShippingCarrierLatestCreateOperationView? LatestCreateOperation,
    IReadOnlyList<ShippingCarrierExecutableLineView> ExecutableLines);

/// <summary>Remaining packed line quantity available for the next carrier dispatch.</summary>
public sealed record ShippingCarrierExecutableLineView(Guid OrderLineId, decimal RemainingPackedQuantity);

/// <summary>Requested line allocation for one physical carrier package/dispatch.</summary>
public sealed record ShippingCarrierShipmentLineQuantity(Guid OrderLineId, decimal Quantity);

/// <summary>Authorized operational package completion without mutating Order accepted evidence.</summary>
public sealed record ShippingCarrierShipmentPackageCompletionRequest(
    Guid CheckoutId,
    Guid ShippingGroupId,
    decimal WeightGrams,
    decimal DeclaredValue,
    decimal LengthCentimeters,
    decimal WidthCentimeters,
    decimal HeightCentimeters);

/// <summary>Authorized durable sender completion separate from carrier shipment create.</summary>
public sealed record ShippingCarrierShipmentSenderCompletionRequest(
    Guid CheckoutId,
    Guid ShippingGroupId,
    string FullName,
    string Mobile,
    string AddressLine,
    string PostalCode);

/// <summary>بستهٔ فیزیکی برای ایجاد محموله؛ مبلغ collect جدا از کرایهٔ تسویه است.</summary>
/// <param name="WeightGrams">وزن به گرم.</param>
/// <param name="DeclaredValue">ارزش اظهارشده.</param>
/// <param name="LengthCentimeters">طول.</param>
/// <param name="WidthCentimeters">عرض.</param>
/// <param name="HeightCentimeters">ارتفاع.</param>
/// <param name="CollectAmount">مبلغ جمع‌آوری در محل؛ جدا از مبلغ تسویه.</param>
public sealed record ShippingCarrierShipmentPackageInput(
    decimal WeightGrams,
    decimal DeclaredValue,
    decimal LengthCentimeters,
    decimal WidthCentimeters,
    decimal HeightCentimeters,
    decimal CollectAmount);

/// <summary>Actual physical package facts for one dispatch; never inferred by summing product dimensions.</summary>
public sealed record ShippingCarrierShipmentDispatchPackageInput(
    decimal WeightGrams,
    decimal LengthCentimeters,
    decimal WidthCentimeters,
    decimal HeightCentimeters);

/// <summary>Execute carrier shipment create; account, service, sender, package, and recipient are server-owned.</summary>
/// <param name="CheckoutId">Committed checkout.</param>
/// <param name="ShippingGroupId">Accepted shipping group.</param>
/// <param name="Lines">Exact line quantities for this physical package; empty/null uses all remaining executable lines.</param>
/// <param name="Package">Per-dispatch weight/dimensions. Required for partial packages; full-remaining may reuse group completion.</param>
/// <param name="PackageDispatchId">Optional untrusted client locator; server validates against durable intent.</param>
public sealed record ShippingCarrierShipmentExecuteRequest(
    Guid CheckoutId,
    Guid ShippingGroupId,
    IReadOnlyList<ShippingCarrierShipmentLineQuantity>? Lines = null,
    ShippingCarrierShipmentDispatchPackageInput? Package = null,
    Guid? PackageDispatchId = null);
