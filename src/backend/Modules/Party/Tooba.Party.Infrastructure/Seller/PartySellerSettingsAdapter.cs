using Tooba.Party.Application.Models;
using Tooba.Party.Application.Ports;
using Tooba.Party.Contracts.Ports;

namespace Tooba.Party.Infrastructure.Seller;

/// <summary>
/// آداپتر مالک Party برای درز پایدار تنظیمات فروشنده.
/// <para>
/// نوشتن/خواندن را روی دایرکتوری خود ماژول انجام می‌دهد؛ هیچ DbContext خارجی و هیچ
/// Application/Domain ماژول دیگری اینجا نیست. سیاست مجوز اینجا نوشته نمی‌شود.
/// </para>
/// </summary>
public sealed class PartySellerSettingsAdapter(IPartyDirectory directory) : IPartySellerSettings
{
    /// <inheritdoc />
    public async Task<PartySellerSettingsSnapshot?> GetAsync(Guid partyId, CancellationToken cancellationToken)
    {
        var snapshot = await directory.GetOrganizationProfileAsync(partyId, cancellationToken);
        return snapshot is null ? null : Map(snapshot);
    }

    /// <inheritdoc />
    public async Task<PartySellerSettingsSnapshot> UpdateAsync(
        Guid partyId,
        PartySellerSettingsWrite input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var updated = await directory.UpdateOrganizationProfileAsync(
            partyId,
            new OrganizationProfileWrite(
                input.DisplayName,
                input.LegalName,
                input.Description,
                input.SupportPhone,
                input.SupportEmail,
                input.AddressLine),
            cancellationToken);
        return Map(updated);
    }

    private static PartySellerSettingsSnapshot Map(OrganizationProfileSnapshot snapshot) =>
        new(
            snapshot.PartyId,
            snapshot.DisplayName,
            snapshot.LegalName,
            snapshot.Description,
            snapshot.SupportPhone,
            snapshot.SupportEmail,
            snapshot.AddressLine,
            snapshot.UpdatedAt);
}
