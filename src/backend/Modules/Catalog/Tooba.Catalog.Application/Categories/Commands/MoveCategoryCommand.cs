using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Categories.Commands;

/// <summary>Moves a category under a new parent and returns workspace.</summary>
public sealed record MoveCategoryCommand(
    Guid CategoryId,
    Guid? NewParentId,
    DateTimeOffset? ExpectedUpdatedAt)
    : IRequest<Result<CategoryWorkspaceSummaryDto>>;
