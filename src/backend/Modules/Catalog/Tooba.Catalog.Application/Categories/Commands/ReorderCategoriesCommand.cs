using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application.Categories.Models;

namespace Tooba.Catalog.Application.Categories.Commands;

/// <summary>Reorders siblings under a parent.</summary>
public sealed record ReorderCategoriesCommand(Guid? ParentId, IReadOnlyList<Guid>? OrderedCategoryIds)
    : IRequest<Result<CategoryOkResult>>;
