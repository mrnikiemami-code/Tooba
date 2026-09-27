using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.MegaMenu.Ports;

namespace Tooba.Catalog.Application.MegaMenu.Queries;

/// <summary>Loads Admin MegaMenu configuration for a category.</summary>
public sealed class GetCategoryMegaMenuHandler
    : IRequestHandler<GetCategoryMegaMenuQuery, Result<CategoryMegaMenuConfigurationView>>
{
    private readonly IMegaMenuDirectory _megaMenu;

    /// <summary>Creates the handler.</summary>
    public GetCategoryMegaMenuHandler(IMegaMenuDirectory megaMenu) => _megaMenu = megaMenu;

    /// <inheritdoc />
    public Task<Result<CategoryMegaMenuConfigurationView>> Handle(
        GetCategoryMegaMenuQuery request,
        CancellationToken cancellationToken)
    {
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "fa-IR" : request.Locale;
        return _megaMenu.GetCategoryConfigurationAsync(request.CategoryId, locale, cancellationToken);
    }
}
