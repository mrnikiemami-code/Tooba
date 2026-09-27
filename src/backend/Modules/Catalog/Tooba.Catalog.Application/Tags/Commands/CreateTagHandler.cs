using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Catalog.Application;
using Tooba.Catalog.Application.Tags.Ports;

namespace Tooba.Catalog.Application.Tags.Commands;

/// <summary>Creates a Catalog taxonomy tag.</summary>
public sealed class CreateTagHandler : IRequestHandler<CreateTagCommand, Result<TagView>>
{
    private readonly ITagDirectory _tags;

    /// <summary>Creates the handler.</summary>
    public CreateTagHandler(ITagDirectory tags) => _tags = tags;

    /// <inheritdoc />
    public Task<Result<TagView>> Handle(CreateTagCommand request, CancellationToken cancellationToken) =>
        _tags.CreateAsync(request.Model, cancellationToken);
}
