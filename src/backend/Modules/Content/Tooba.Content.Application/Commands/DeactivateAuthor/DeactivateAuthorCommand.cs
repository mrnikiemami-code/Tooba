using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.DeactivateAuthor;

public sealed record DeactivateAuthorCommand(Guid AuthorId) : IRequest<Result>;

public sealed class DeactivateAuthorCommandHandler(IContentAuthorDirectory authors)
    : IRequestHandler<DeactivateAuthorCommand, Result>
{
    public Task<Result> Handle(DeactivateAuthorCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => authors.DeactivateAsync(request.AuthorId, cancellationToken),
            ContentErrorCodes.UpdateRejected);
}
