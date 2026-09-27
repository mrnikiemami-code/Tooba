using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.ProductMedia.Models;

namespace Tooba.Catalog.Application.ProductMedia.Queries;

/// <summary>List product media references ordered primary-first then DisplayOrder.</summary>
public sealed record GetProductMediaQuery(Guid ProductId)
    : IRequest<Result<IReadOnlyList<ProductMediaItemView>>>;
