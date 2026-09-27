using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Articles.Models;
using Tooba.Content.Application.Articles.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Articles.Commands;

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
            () => content.CreateAsync(
                request with { Tags = request.Tags ?? Array.Empty<string>() },
                cancellationToken),
            ContentErrorCodes.CreateRejected);
}
