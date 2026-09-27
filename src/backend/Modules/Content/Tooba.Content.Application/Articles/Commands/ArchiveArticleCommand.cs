using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Articles.Models;
using Tooba.Content.Application.Articles.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.BuildingBlocks.Grid;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Articles.Commands;

public sealed record ArchiveArticleCommand(Guid ArticleId) : IRequest<Result<AdminArticleSnapshot>>;

public sealed class ArchiveArticleCommandHandler(IContentDirectory content)
    : IRequestHandler<ArchiveArticleCommand, Result<AdminArticleSnapshot>>
{
    public Task<Result<AdminArticleSnapshot>> Handle(ArchiveArticleCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(() => content.ArchiveAsync(request.ArticleId, cancellationToken), ContentErrorCodes.UpdateRejected);
}
