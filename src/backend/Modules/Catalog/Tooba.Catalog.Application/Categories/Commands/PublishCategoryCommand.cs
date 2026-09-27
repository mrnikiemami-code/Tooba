using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Categories.Commands;

/// <summary>Publishes a category and returns workspace.</summary>
public sealed record PublishCategoryCommand(Guid CategoryId)
    : IRequest<Result<CategoryWorkspaceSummaryDto>>;
