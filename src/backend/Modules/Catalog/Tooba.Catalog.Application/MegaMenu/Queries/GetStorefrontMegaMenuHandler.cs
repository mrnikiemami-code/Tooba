using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.MegaMenu.Ports;

namespace Tooba.Catalog.Application.MegaMenu.Queries;

/// <summary>Loads composed storefront MegaMenu.</summary>
public sealed class GetStorefrontMegaMenuHandler
    : IRequestHandler<GetStorefrontMegaMenuQuery, Result<IReadOnlyList<StorefrontMegaMenuItem>>>
{
    private readonly IMegaMenuDirectory _megaMenu;

    /// <summary>Creates the handler.</summary>
    public GetStorefrontMegaMenuHandler(IMegaMenuDirectory megaMenu) => _megaMenu = megaMenu;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<StorefrontMegaMenuItem>>> Handle(
        GetStorefrontMegaMenuQuery request,
        CancellationToken cancellationToken)
    {
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "fa-IR" : request.Locale;
        var items = await _megaMenu.GetStorefrontMenuAsync(locale, cancellationToken);
        return Result.Success(items);
    }
}
