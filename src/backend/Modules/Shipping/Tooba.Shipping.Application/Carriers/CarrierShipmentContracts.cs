namespace Tooba.Shipping.Application.Carriers;

public enum CarrierShipmentOperationKind
{
    Create = 0,
    Track = 1,
    Cancel = 2,
    Label = 3
}

public enum CarrierShipmentOutcome
{
    Succeeded = 0,
    Unsupported = 1,
    Unconfigured = 2,
    NotEligible = 3,
    RequestInvalid = 4,
    Unauthorized = 5,
    ProviderRejected = 6,
    TemporarilyUnavailable = 7,
    Unknown = 8,
    NotFound = 9
}

public sealed record CarrierShipmentCapabilities(
    string ProviderCode,
    bool CanCreate,
    bool CanTrack,
    bool CanCancel,
    bool CanLabel);

public sealed record CarrierShipmentParty(
    string FullName,
    string Mobile,
    string AddressLine,
    string LocationCode,
    string? PostalCode = null);

public sealed record CarrierShipmentPackage(
    decimal WeightGrams,
    decimal DeclaredValue,
    decimal LengthCentimeters,
    decimal WidthCentimeters,
    decimal HeightCentimeters,
    decimal CollectAmount);

public sealed record CarrierShipmentCreateRequest(
    string OperationKey,
    Guid AccountId,
    int ConfigurationVersion,
    CarrierAccountExecution Account,
    string ServiceCode,
    CarrierShipmentParty Sender,
    CarrierShipmentParty Recipient,
    CarrierShipmentPackage Package,
    string SuccessCallbackUrl,
    string FailureCallbackUrl);

public sealed record CarrierShipmentTrackRequest(
    string OperationKey,
    CarrierAccountExecution Account,
    string? TrackingCode,
    string? SecondaryTraceCode);

public sealed record CarrierShipmentCancelRequest(
    string OperationKey,
    CarrierAccountExecution Account,
    string TrackingCode);

public sealed record CarrierShipmentResult(
    CarrierShipmentOutcome Outcome,
    string? ReasonCode = null,
    string? TrackingCode = null,
    string? ProviderOrderId = null,
    string? ProviderStatus = null,
    string? ProviderReference = null)
{
    public static CarrierShipmentResult Failure(CarrierShipmentOutcome outcome, string reasonCode)
        => new(outcome, reasonCode);
}
