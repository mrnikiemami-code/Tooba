using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Seller.Models;
using Tooba.Catalog.Application.Seller.Ports;
using Tooba.Party.Contracts;

namespace Tooba.Catalog.Application.Seller.Queries;

/// <summary>Handles ListSellerCatalogVariantsQuery.</summary>
public sealed class ListSellerCatalogVariantsHandler(
    ISellerCatalogVariantDirectory directory,
    IPartyLookup parties)
    : IRequestHandler<ListSellerCatalogVariantsQuery, Result<IReadOnlyList<SellerCatalogVariantOption>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<SellerCatalogVariantOption>>> Handle(
        ListSellerCatalogVariantsQuery request,
        CancellationToken cancellationToken)
    {
        if (await parties.FindByIdAsync(request.SellerPartyId, cancellationToken) is null)
        {
            return Result.Failure<IReadOnlyList<SellerCatalogVariantOption>>(
                new SemanticError(SellerCatalogErrorCodes.SellerMissing));
        }

        return Result.Success(await directory.ListPublishedVariantsAsync(cancellationToken));
    }
}
