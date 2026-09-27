using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Brands.Models;

namespace Tooba.Catalog.Application.Brands.Queries;

/// <summary>Admin GET brand-options (optional free-text <c>q</c>).</summary>
public sealed record ListBrandOptionsQuery(string? Search)
    : IRequest<Result<IReadOnlyList<BrandOptionView>>>;
