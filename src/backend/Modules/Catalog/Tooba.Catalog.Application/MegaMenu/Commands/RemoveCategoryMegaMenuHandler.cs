using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.MegaMenu.Ports;

namespace Tooba.Catalog.Application.MegaMenu.Commands;

/// <summary>Removes category MegaMenu binding (idempotent).</summary>
public sealed class RemoveCategoryMegaMenuHandler : IRequestHandler<RemoveCategoryMegaMenuCommand, Result>
{
    private readonly IMegaMenuDirectory _megaMenu;

    /// <summary>Creates the handler.</summary>
    public RemoveCategoryMegaMenuHandler(IMegaMenuDirectory megaMenu) => _megaMenu = megaMenu;

    /// <inheritdoc />
    public Task<Result> Handle(RemoveCategoryMegaMenuCommand request, CancellationToken cancellationToken) =>
        _megaMenu.RemoveBindingAsync(request.CategoryId, cancellationToken);
}
