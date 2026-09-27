using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Models;
using Tooba.Content.Application.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Domain.Aggregates;
using Tooba.Content.Domain.Rules;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Queries.ListPublicAuthors;

public sealed record ListPublicAuthorsQuery(string? Locale)
    : IRequest<Result<IReadOnlyList<PublishedContentAuthorItem>>>;

public sealed class ListPublicAuthorsQueryHandler(IContentAuthorDirectory authors)
    : IRequestHandler<ListPublicAuthorsQuery, Result<IReadOnlyList<PublishedContentAuthorItem>>>
{
    public Task<Result<IReadOnlyList<PublishedContentAuthorItem>>> Handle(
        ListPublicAuthorsQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => authors.ListPublicAsync(ContentTaxonomySeoRules.ResolveContentLocale(request.Locale), cancellationToken),
            ContentErrorCodes.AuthorNotFound);
}
