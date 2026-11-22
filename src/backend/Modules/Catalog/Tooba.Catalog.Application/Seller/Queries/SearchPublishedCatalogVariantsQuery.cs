using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Seller.Models;
using Tooba.Catalog.Application.Seller.Ports;
using Tooba.Catalog.Contracts;
using Tooba.Party.Contracts.Ports;

namespace Tooba.Catalog.Application.Seller.Queries;

/// <summary>Published Catalog variant picker search/page. SellerPartyId is null only for an authorized admin.</summary>
public sealed record SearchPublishedCatalogVariantsQuery(
    Guid? SellerPartyId,
    string? Search,
    int Page,
    int PageSize) : IRequest<Result<SellerCatalogVariantPage>>;

/// <summary>Validates the seller scope when present and delegates the SQL page query.</summary>
public sealed class SearchPublishedCatalogVariantsHandler(
    ISellerCatalogVariantDirectory directory,
    IPartyLookup parties) : IRequestHandler<SearchPublishedCatalogVariantsQuery, Result<SellerCatalogVariantPage>>
{
    /// <summary>Returns a bounded published variant page.</summary>
    public async Task<Result<SellerCatalogVariantPage>> Handle(
        SearchPublishedCatalogVariantsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.SellerPartyId is Guid seller
            && await parties.FindByIdAsync(seller, cancellationToken) is null)
        {
            return Result.Failure<SellerCatalogVariantPage>(
                new SemanticError(SellerCatalogErrorCodes.SellerMissing));
        }
        return Result.Success(await directory.SearchPublishedVariantsAsync(
            request.Search,
            request.Page,
            request.PageSize,
            cancellationToken));
    }
}

