using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductSeo.Models;

namespace Tooba.Catalog.Application.ProductSeo.Queries;

/// <summary>Admin GET product SEO detail for a locale.</summary>
public sealed record GetProductSeoQuery(Guid ProductId, string? Locale)
    : IRequest<Result<ProductSeoDetailView>>;
