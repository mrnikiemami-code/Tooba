using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Tags.Models;
using Tooba.Content.Application.Tags.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Tags.Commands;

public sealed record CreateTagCommand(string LanguageCode, string Name, string? Slug)
    : IRequest<Result<ContentTagDto>>;

public sealed class CreateTagCommandHandler(IContentTagDirectory tags)
    : IRequestHandler<CreateTagCommand, Result<ContentTagDto>>
{
    public Task<Result<ContentTagDto>> Handle(CreateTagCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => tags.CreateAsync(request, cancellationToken),
            ContentErrorCodes.CreateRejected);
}
