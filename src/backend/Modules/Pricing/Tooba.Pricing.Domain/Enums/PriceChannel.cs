namespace Tooba.Pricing.Domain;

/// <summary>
/// Sales channel stored with an authored price. Names match Offer channel labels so existing rows stay valid.
/// </summary>
public enum PriceChannel
{
    /// <summary>Direct store sales.</summary>
    Direct = 0,

    /// <summary>Multi-seller marketplace.</summary>
    Marketplace = 1,

    /// <summary>Agency sales.</summary>
    Agency = 2,

    /// <summary>Corporate sales.</summary>
    Corporate = 3,

    /// <summary>Affiliate sales.</summary>
    Affiliate = 4,

    /// <summary>Integrated API sales.</summary>
    Api = 5,
}
