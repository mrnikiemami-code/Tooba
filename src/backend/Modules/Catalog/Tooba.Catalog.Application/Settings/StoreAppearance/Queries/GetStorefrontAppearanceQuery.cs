using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Settings.StoreAppearance.Models;
using Tooba.Catalog.Application.Settings.StoreAppearance.Ports;

namespace Tooba.Catalog.Application.Settings.StoreAppearance.Queries;

/// <summary>Storefront GET /v1/storefront/appearance envelope (parity with Host residual).</summary>
public sealed record StorefrontAppearanceDto(
    string StoreScope,
    string PaletteKey,
    bool PaletteKeyWasKnown,
    string ThemeMode,
    string ProductCardSkin,
    string BackgroundStyle,
    StorefrontAppearanceTokensDto Tokens,
    StorefrontAppearanceTintDto Tint,
    DateTimeOffset UpdatedAt);

/// <summary>Brand token RGB strings.</summary>
public sealed record StorefrontAppearanceTokensDto(
    string PrimaryRgb,
    string PrimaryStrongRgb,
    string OnPrimaryRgb,
    string FocusRgb,
    string PrimaryOnDarkRgb);

/// <summary>Page/section tint RGB strings.</summary>
public sealed record StorefrontAppearanceTintDto(
    string PageBackgroundRgb,
    string SectionBackgroundRgb,
    string SectionSurfaceRgb,
    string SectionAlternateRgb,
    string SectionAccentRgb,
    string PageBackgroundDarkRgb,
    string SectionBackgroundDarkRgb,
    string SectionSurfaceDarkRgb,
    string SectionAlternateDarkRgb,
    string SectionAccentDarkRgb);

/// <summary>Loads effective storefront appearance projection.</summary>
public sealed record GetStorefrontAppearanceQuery : IRequest<Result<StorefrontAppearanceDto>>;

/// <summary>Handler for <see cref="GetStorefrontAppearanceQuery"/>.</summary>
public sealed class GetStorefrontAppearanceHandler(IStoreAppearanceProjector projector)
    : IRequestHandler<GetStorefrontAppearanceQuery, Result<StorefrontAppearanceDto>>
{
    /// <inheritdoc />
    public async Task<Result<StorefrontAppearanceDto>> Handle(
        GetStorefrontAppearanceQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var appearance = await projector.GetEffectiveAsync(cancellationToken);
        return Result.Success(Map(appearance));
    }

    private static StorefrontAppearanceDto Map(StoreAppearanceProjection appearance) =>
        new(
            appearance.StoreScope,
            appearance.PaletteKey,
            appearance.PaletteKeyWasKnown,
            appearance.ThemeMode,
            appearance.ProductCardSkin,
            appearance.BackgroundStyle,
            new StorefrontAppearanceTokensDto(
                appearance.PrimaryRgb,
                appearance.PrimaryStrongRgb,
                appearance.OnPrimaryRgb,
                appearance.FocusRgb,
                appearance.PrimaryOnDarkRgb),
            new StorefrontAppearanceTintDto(
                appearance.PageBackgroundRgb,
                appearance.SectionBackgroundRgb,
                appearance.SectionBackgroundRgb,
                appearance.SectionAlternateRgb,
                appearance.SectionAccentRgb,
                appearance.PageBackgroundDarkRgb,
                appearance.SectionBackgroundDarkRgb,
                appearance.SectionBackgroundDarkRgb,
                appearance.SectionAlternateDarkRgb,
                appearance.SectionAccentDarkRgb),
            appearance.UpdatedAt);
}
