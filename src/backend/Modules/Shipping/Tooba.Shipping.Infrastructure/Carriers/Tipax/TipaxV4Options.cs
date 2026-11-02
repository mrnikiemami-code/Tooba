namespace Tooba.Shipping.Infrastructure.Carriers.Tipax;

/// <summary>
/// Tipax V4 credentials are configuration/secret-store values. No credential is persisted in Shipping.
/// The provider is fail-closed until Enabled and every required value are supplied.
/// </summary>
internal sealed class TipaxV4Options
{
    public const string SectionName = "Tooba:Shipping:Carriers:Tipax";

    public bool Enabled { get; set; }

    public string ApiBaseUrl { get; set; } = "https://omtestapi.tipax.ir";

    public string TokenUrl { get; set; } = "https://igtgatewayapi.tipax.ir/Token";

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string CurrencyCode { get; set; } = "IRR";

    public int PrepaidPaymentTypeId { get; set; }

    public int CollectPaymentTypeId { get; set; }

    public int CashOnDeliveryPaymentTypeId { get; set; }

    public int DoorPickupTypeId { get; set; }

    public int DropOffPickupTypeId { get; set; }

    public int DoorDistributionTypeId { get; set; }

    public int BranchDistributionTypeId { get; set; }

    public int PackingId { get; set; }

    public int PackageContentId { get; set; }

    public int PackType { get; set; }

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(8);

    internal bool IsValid()
        => Enabled
           && Uri.TryCreate(ApiBaseUrl, UriKind.Absolute, out _)
           && Uri.TryCreate(TokenUrl, UriKind.Absolute, out _)
           && !string.IsNullOrWhiteSpace(Username)
           && !string.IsNullOrWhiteSpace(Password)
           && !string.IsNullOrWhiteSpace(CurrencyCode)
           && Timeout > TimeSpan.Zero
           && PrepaidPaymentTypeId > 0
           && CollectPaymentTypeId > 0
           && CashOnDeliveryPaymentTypeId > 0
           && DoorPickupTypeId > 0
           && DropOffPickupTypeId > 0
           && DoorDistributionTypeId > 0
           && BranchDistributionTypeId > 0
           && PackingId >= 0
           && PackageContentId >= 0
           && PackType >= 0;
}
