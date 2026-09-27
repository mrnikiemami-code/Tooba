using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Categories.Commands;

/// <summary>Archives a category and returns workspace.</summary>
public sealed record ArchiveCategoryCommand(Guid CategoryId)
    : IRequest<Result<CategoryWorkspaceSummaryDto>>;
