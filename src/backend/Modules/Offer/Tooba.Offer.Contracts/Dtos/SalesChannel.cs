namespace Tooba.Offer.Contracts.Dtos;

/// <summary>
/// Public sales channel representation.
/// </summary>
public enum SalesChannel
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
