using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.MegaMenu.Ports;

namespace Tooba.Catalog.Application.MegaMenu.Commands;

/// <summary>Upserts category MegaMenu binding.</summary>
public sealed class UpsertCategoryMegaMenuHandler : IRequestHandler<UpsertCategoryMegaMenuCommand, Result>
{
    private readonly IMegaMenuDirectory _megaMenu;

    /// <summary>Creates the handler.</summary>
    public UpsertCategoryMegaMenuHandler(IMegaMenuDirectory megaMenu) => _megaMenu = megaMenu;

    /// <inheritdoc />
    public Task<Result> Handle(UpsertCategoryMegaMenuCommand request, CancellationToken cancellationToken)
    {
        var locale = string.IsNullOrWhiteSpace(request.Locale) ? "fa-IR" : request.Locale;
        return _megaMenu.UpsertBindingAsync(request.CategoryId, locale, request.Input, cancellationToken);
    }
}
