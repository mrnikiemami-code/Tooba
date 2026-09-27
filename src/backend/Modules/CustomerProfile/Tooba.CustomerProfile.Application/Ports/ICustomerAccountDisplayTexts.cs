namespace Tooba.CustomerProfile.Application.Ports;

/// <summary>
/// Presentation display texts for customer-account composition. Implemented by Endpoints resources;
/// Application never embeds user-facing literals.
/// </summary>
public interface ICustomerAccountDisplayTexts
{
    /// <summary>Localized default display name when profile and order recipient are absent.</summary>
    string DefaultDisplayName { get; }
}
