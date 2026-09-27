using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;

namespace Tooba.Content.Application.Commands.CreateArticle;

public sealed record CreateArticleCommand(
    string Slug, string Title, string Excerpt, string Body,
    Guid? CoverMediaAssetId, Guid? AuthorId, IReadOnlyList<string>? Tags, bool IsFeatured,
    DateTimeOffset? PublishDate, string? Locale, string? SeoTitle, string? SeoDescription,
    string? Category, Guid? CategoryId) : IRequest<Result<AdminArticleSnapshot>>;

public sealed class CreateArticleCommandHandler(IContentDirectory content)
    : IRequestHandler<CreateArticleCommand, Result<AdminArticleSnapshot>>
{
    public Task<Result<AdminArticleSnapshot>> Handle(CreateArticleCommand request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => content.CreateAsync(new Tooba.Content.Application.Models.CreateArticleCommand(
                request.Slug, request.Title, request.Excerpt, request.Body,
                request.CoverMediaAssetId, request.AuthorId, request.Tags ?? [],
                request.IsFeatured, request.PublishDate, request.Locale,
                request.SeoTitle, request.SeoDescription, request.Category, request.CategoryId), cancellationToken),
            ContentErrorCodes.CreateRejected);
}
