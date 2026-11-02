namespace Tooba.Shipping.Application.Carriers;

/// <summary>
/// A provider-neutral request for one carrier rate. Provider identifiers are opaque strings so adding a
/// carrier never changes Shipping's domain model or Checkout contracts.
/// </summary>
public sealed record CarrierRateRequest(
    string RequestId,
    string OriginLocationCode,
    string DestinationLocationCode,
    string ServiceCode,
    decimal WeightGrams,
    decimal DeclaredValue,
    decimal LengthCentimeters,
    decimal WidthCentimeters,
    decimal HeightCentimeters,
    CarrierPaymentKind PaymentKind = CarrierPaymentKind.Prepaid,
    CarrierPickupKind PickupKind = CarrierPickupKind.DoorPickup,
    CarrierDeliveryKind DeliveryKind = CarrierDeliveryKind.DoorDelivery,
    string? DiscountCode = null,
    CarrierAccountExecution? Account = null);

/// <summary>
/// اجرای حساب‌محور: راز و شناسه‌های مرجع از پیکربندی مالک می‌آیند، نه از options سراسری.
/// </summary>
public sealed record CarrierAccountExecution(
    Guid AccountId,
    int ConfigurationVersion,
    string Username,
    string Password,
    string CurrencyCode,
    string PaymentTypeId,
    string PickupTypeId,
    string DistributionTypeId,
    string PackingId,
    string PackageContentId,
    string PackTypeId,
    string? ApiBaseUrl = null,
    string? TokenUrl = null);

public enum CarrierPaymentKind
{
    Prepaid = 0,
    CollectOnDelivery = 1,
    CashOnDelivery = 2
}

public enum CarrierPickupKind
{
    DoorPickup = 0,
    DropOff = 1
}

public enum CarrierDeliveryKind
{
    DoorDelivery = 0,
    BranchPickup = 1
}

public enum CarrierRateOutcome
{
    Quoted = 0,
    Disabled = 1,
    ConfigurationInvalid = 2,
    RequestInvalid = 3,
    Unauthorized = 4,
    NoService = 5,
    TemporarilyUnavailable = 6,
    ProviderRejected = 7
}

/// <summary>A normalized quote option. Provider-specific response types never cross this boundary.</summary>
/// <remarks>
/// <see cref="ProviderReference"/> is a provider-issued quote/reservation id only. Local request
/// correlation is never stored here. Checkout money still comes from the captured quote amounts.
/// </remarks>
public sealed record CarrierRateOption(
    string ProviderCode,
    string ServiceCode,
    string ServiceTitle,
    string CurrencyCode,
    decimal FinalAmount,
    decimal TransportationAmount,
    decimal TaxAmount,
    decimal PackagingAmount,
    decimal PickupAmount,
    decimal DeliveryAmount,
    string ProviderReference);

public sealed record CarrierRateResult(
    CarrierRateOutcome Outcome,
    IReadOnlyList<CarrierRateOption> Options,
    string? ReasonCode = null)
{
    public static CarrierRateResult Failure(CarrierRateOutcome outcome, string reasonCode)
        => new(outcome, Array.Empty<CarrierRateOption>(), reasonCode);
}
