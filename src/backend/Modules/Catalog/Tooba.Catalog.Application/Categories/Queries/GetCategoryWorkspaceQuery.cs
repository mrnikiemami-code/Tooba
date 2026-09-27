using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Categories.Queries;

/// <summary>Reads category workspace summary.</summary>
public sealed record GetCategoryWorkspaceQuery(Guid CategoryId, string? Locale)
    : IRequest<Result<CategoryWorkspaceSummaryDto>>;
