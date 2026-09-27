using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.MegaMenu.Ports;

namespace Tooba.Catalog.Application.MegaMenu.Queries;

/// <summary>Lists MegaMenu placement options for Admin.</summary>
public sealed class ListMegaMenuPlacementOptionsHandler
    : IRequestHandler<ListMegaMenuPlacementOptionsQuery, Result<IReadOnlyList<MegaMenuPlacementOption>>>
{
    private readonly IMegaMenuDirectory _megaMenu;

    /// <summary>Creates the handler.</summary>
    public ListMegaMenuPlacementOptionsHandler(IMegaMenuDirectory megaMenu) => _megaMenu = megaMenu;

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<MegaMenuPlacementOption>>> Handle(
        ListMegaMenuPlacementOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "fa-IR" : request.Locale;
        var options = await _megaMenu.ListPlacementOptionsAsync(request.CategoryId, locale, cancellationToken);
        return Result.Success(options);
    }
}
