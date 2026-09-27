using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Authors.Models;
using Tooba.Content.Application.Authors.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Authors.Queries;

public sealed record GetPublicAuthorBySlugQuery(string Slug, string? Locale)
    : IRequest<Result<PublishedContentAuthorItem>>;

public sealed class GetPublicAuthorBySlugQueryHandler(IContentAuthorDirectory authors)
    : IRequestHandler<GetPublicAuthorBySlugQuery, Result<PublishedContentAuthorItem>>
{
    public async Task<Result<PublishedContentAuthorItem>> Handle(
        GetPublicAuthorBySlugQuery request, CancellationToken cancellationToken)
    {
        var item = await authors.GetPublicBySlugAsync(
            request.Slug, ContentTaxonomySeoRules.ResolveContentLocale(request.Locale), cancellationToken);
        return ContentOperation.NotFoundIfNull(item, ContentErrorCodes.AuthorNotFound);
    }
}
