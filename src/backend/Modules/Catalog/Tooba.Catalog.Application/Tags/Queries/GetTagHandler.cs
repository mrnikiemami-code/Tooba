using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Tags.Ports;

namespace Tooba.Catalog.Application.Tags.Queries;

/// <summary>Gets one Catalog tag.</summary>
public sealed class GetTagHandler : IRequestHandler<GetTagQuery, Result<TagView>>
{
    private readonly ITagDirectory _tags;

    /// <summary>Creates the handler.</summary>
    public GetTagHandler(ITagDirectory tags) => _tags = tags;

    /// <inheritdoc />
    public Task<Result<TagView>> Handle(GetTagQuery request, CancellationToken cancellationToken) =>
        _tags.GetAsync(request.TagId, request.Locale, cancellationToken);
}
