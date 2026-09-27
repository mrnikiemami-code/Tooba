using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;

namespace Tooba.Catalog.Application.Tags.Commands;

/// <summary>POST /v1/admin/catalog/products/{productId}/tags/{tagId}</summary>
public sealed record AssignProductTagCommand(Guid ProductId, Guid TagId)
    : IRequest<Result<IReadOnlyList<TagView>>>;
