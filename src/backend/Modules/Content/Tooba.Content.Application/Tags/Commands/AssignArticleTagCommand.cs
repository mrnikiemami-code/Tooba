using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Tags.Models;
using Tooba.Content.Application.Tags.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Tags.Commands;

public sealed record AssignArticleTagCommand(Guid ArticleId, Guid TagId)
    : IRequest<Result<IReadOnlyList<ContentTagDto>>>;

public sealed class AssignArticleTagCommandHandler(IContentTagDirectory tags)
    : IRequestHandler<AssignArticleTagCommand, Result<IReadOnlyList<ContentTagDto>>>
{
    public Task<Result<IReadOnlyList<ContentTagDto>>> Handle(
        AssignArticleTagCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => tags.AssignToArticleAsync(request.ArticleId, request.TagId, cancellationToken),
            ContentErrorCodes.UpdateRejected);
}
