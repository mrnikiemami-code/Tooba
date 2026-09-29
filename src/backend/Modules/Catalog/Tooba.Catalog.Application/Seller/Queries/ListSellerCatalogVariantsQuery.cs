using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Seller.Models;

namespace Tooba.Catalog.Application.Seller.Queries;

/// <summary>Lists published Catalog variants available for seller offer creation.</summary>
public sealed record ListSellerCatalogVariantsQuery(Guid SellerPartyId)
    : IRequest<Result<IReadOnlyList<SellerCatalogVariantOption>>>;
