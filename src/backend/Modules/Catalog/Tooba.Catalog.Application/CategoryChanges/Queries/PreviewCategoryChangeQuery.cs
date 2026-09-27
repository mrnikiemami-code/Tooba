using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.CategoryChanges.Models;

namespace Tooba.Catalog.Application.CategoryChanges.Queries;

/// <summary>Preview impact of changing a product primary category.</summary>
public sealed record PreviewCategoryChangeQuery(Guid ProductId, CategoryChangePreviewWriteModel Model)
    : IRequest<Result<CategoryChangeImpactReport>>;
