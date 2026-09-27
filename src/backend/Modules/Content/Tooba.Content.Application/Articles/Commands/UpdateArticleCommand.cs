using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Articles.Models;
using Tooba.Content.Application.Articles.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Articles.Commands;

public sealed record UpdateArticleCommand(
    Guid ArticleId, string Title, string Excerpt, string Body,
    Guid? CoverMediaAssetId, Guid? AuthorId, IReadOnlyList<string>? Tags, bool IsFeatured,
    string? Locale, string? SeoTitle, string? SeoDescription, string? Category, Guid? CategoryId,
    DateTimeOffset? PublishDate) : IRequest<Result<AdminArticleSnapshot>>;

public sealed class UpdateArticleCommandHandler(IContentDirectory content)
    : IRequestHandler<UpdateArticleCommand, Result<AdminArticleSnapshot>>
{
    public Task<Result<AdminArticleSnapshot>> Handle(UpdateArticleCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => content.UpdateAsync(
                request with { Tags = request.Tags ?? Array.Empty<string>() },
                cancellationToken),
            ContentErrorCodes.UpdateRejected);
}
