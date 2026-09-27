using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Tags.Commands;

/// <summary>POST /v1/admin/catalog/categories/{categoryId}/tags/{tagId}</summary>
public sealed record AssignCategoryTagCommand(Guid CategoryId, Guid TagId)
    : IRequest<Result<IReadOnlyList<TagView>>>;
