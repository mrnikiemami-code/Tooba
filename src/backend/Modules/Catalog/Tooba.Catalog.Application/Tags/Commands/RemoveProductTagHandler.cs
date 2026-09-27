using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Tags.Ports;

namespace Tooba.Catalog.Application.Tags.Commands;

/// <summary>Removes a product-tag assignment (idempotent).</summary>
public sealed class RemoveProductTagHandler
    : IRequestHandler<RemoveProductTagCommand, Result<IReadOnlyList<TagView>>>
{
    private readonly ITagDirectory _tags;

    /// <summary>Creates the handler.</summary>
    public RemoveProductTagHandler(ITagDirectory tags) => _tags = tags;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<TagView>>> Handle(
        RemoveProductTagCommand request,
        CancellationToken cancellationToken) =>
        _tags.RemoveProductTagAsync(request.ProductId, request.TagId, cancellationToken);
}
