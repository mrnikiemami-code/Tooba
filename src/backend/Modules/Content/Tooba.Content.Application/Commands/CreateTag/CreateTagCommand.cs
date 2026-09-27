using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.CreateTag;

public sealed record CreateTagCommand(string LanguageCode, string Name, string? Slug)
    : IRequest<Result<ContentTagDto>>;

public sealed class CreateTagCommandHandler(IContentTagDirectory tags)
    : IRequestHandler<CreateTagCommand, Result<ContentTagDto>>
{
    public Task<Result<ContentTagDto>> Handle(CreateTagCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => tags.CreateAsync(new CreateContentTagCommand(request.LanguageCode, request.Name, request.Slug), cancellationToken),
            ContentErrorCodes.CreateRejected);
}
