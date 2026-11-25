using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Seller.Models;
using Tooba.Catalog.Application.Seller.Ports;
using Tooba.Catalog.Contracts;
using Tooba.Party.Contracts.Ports;

namespace Tooba.Catalog.Application.Seller.Queries;

/// <summary>Published Catalog product picker search/page for the offer create form.</summary>
public sealed record SearchPublishedProductsQuery(
    Guid? SellerPartyId,
    string? Search,
    int Page,
    int PageSize,
    string? Locale) : IRequest<Result<CatalogPickerProductPage>>;

/// <summary>Validates the seller scope when present and delegates the SQL page query.</summary>
public sealed class SearchPublishedProductsHandler(
    ISellerCatalogVariantDirectory directory,
    IPartyLookup parties) : IRequestHandler<SearchPublishedProductsQuery, Result<CatalogPickerProductPage>>
{
    /// <inheritdoc />
    public async Task<Result<CatalogPickerProductPage>> Handle(
        SearchPublishedProductsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.SellerPartyId is Guid seller
            && await parties.FindByIdAsync(seller, cancellationToken) is null)
        {
            return Result.Failure<CatalogPickerProductPage>(
                new SemanticError(SellerCatalogErrorCodes.SellerMissing));
        }

        return Result.Success(await directory.SearchPublishedProductsAsync(
            request.Search,
            request.Page,
            request.PageSize,
            string.IsNullOrWhiteSpace(request.Locale) ? "fa-IR" : request.Locale,
            cancellationToken));
    }
}

/// <summary>Published variants of one product for the offer create form.</summary>
public sealed record ListPublishedProductVariantsQuery(
    Guid ProductId,
    Guid? SellerPartyId,
    string? Locale) : IRequest<Result<IReadOnlyList<CatalogPickerVariantOption>>>;

/// <summary>Validates the seller scope when present and reads human variant labels.</summary>
public sealed class ListPublishedProductVariantsHandler(
    ISellerCatalogVariantDirectory directory,
    IPartyLookup parties) : IRequestHandler<ListPublishedProductVariantsQuery, Result<IReadOnlyList<CatalogPickerVariantOption>>>
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<CatalogPickerVariantOption>>> Handle(
        ListPublishedProductVariantsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.SellerPartyId is Guid seller
            && await parties.FindByIdAsync(seller, cancellationToken) is null)
        {
            return Result.Failure<IReadOnlyList<CatalogPickerVariantOption>>(
                new SemanticError(SellerCatalogErrorCodes.SellerMissing));
        }

        return Result.Success(await directory.ListPublishedVariantsForProductAsync(
            request.ProductId,
            string.IsNullOrWhiteSpace(request.Locale) ? "fa-IR" : request.Locale,
            cancellationToken));
    }
}
