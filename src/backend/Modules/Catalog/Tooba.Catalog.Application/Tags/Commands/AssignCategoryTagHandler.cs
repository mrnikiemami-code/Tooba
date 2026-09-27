using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Tags.Ports;

namespace Tooba.Catalog.Application.Tags.Commands;

/// <summary>Assigns a tag to a category.</summary>
public sealed class AssignCategoryTagHandler
    : IRequestHandler<AssignCategoryTagCommand, Result<IReadOnlyList<TagView>>>
{
    private readonly ITagDirectory _tags;

    /// <summary>Creates the handler.</summary>
    public AssignCategoryTagHandler(ITagDirectory tags) => _tags = tags;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<TagView>>> Handle(
        AssignCategoryTagCommand request,
        CancellationToken cancellationToken) =>
        _tags.AssignCategoryTagAsync(request.CategoryId, request.TagId, cancellationToken);
}
