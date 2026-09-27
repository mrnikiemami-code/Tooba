using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.UpdateArticle;

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
            () => content.UpdateAsync(request.ArticleId, new Tooba.Content.Application.Models.UpdateArticleCommand(
                request.Title, request.Excerpt, request.Body, request.CoverMediaAssetId,
                request.AuthorId, request.Tags ?? [], request.IsFeatured, request.Locale,
                request.SeoTitle, request.SeoDescription, request.Category, request.CategoryId,
                request.PublishDate), cancellationToken),
            ContentErrorCodes.UpdateRejected);
}
