using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Tags.Ports;

namespace Tooba.Catalog.Application.Tags.Commands;

/// <summary>Removes a category-tag assignment (idempotent).</summary>
public sealed class RemoveCategoryTagHandler
    : IRequestHandler<RemoveCategoryTagCommand, Result<IReadOnlyList<TagView>>>
{
    private readonly ITagDirectory _tags;

    /// <summary>Creates the handler.</summary>
    public RemoveCategoryTagHandler(ITagDirectory tags) => _tags = tags;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<TagView>>> Handle(
        RemoveCategoryTagCommand request,
        CancellationToken cancellationToken) =>
        _tags.RemoveCategoryTagAsync(request.CategoryId, request.TagId, cancellationToken);
}
