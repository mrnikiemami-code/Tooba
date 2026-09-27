using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Tags.Ports;

namespace Tooba.Catalog.Application.Tags.Commands;

/// <summary>Assigns a tag to a product.</summary>
public sealed class AssignProductTagHandler
    : IRequestHandler<AssignProductTagCommand, Result<IReadOnlyList<TagView>>>
{
    private readonly ITagDirectory _tags;

    /// <summary>Creates the handler.</summary>
    public AssignProductTagHandler(ITagDirectory tags) => _tags = tags;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<TagView>>> Handle(
        AssignProductTagCommand request,
        CancellationToken cancellationToken) =>
        _tags.AssignProductTagAsync(request.ProductId, request.TagId, cancellationToken);
}
