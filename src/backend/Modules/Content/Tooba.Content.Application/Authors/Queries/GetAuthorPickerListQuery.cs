using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Authors.Models;
using Tooba.Content.Application.Authors.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Authors.Queries;

public sealed record GetAuthorPickerListQuery(string? Search, bool ActiveOnly)
    : IRequest<Result<IReadOnlyList<ContentAuthorPickerItemDto>>>;

public sealed class GetAuthorPickerListQueryHandler(IContentAuthorDirectory authors)
    : IRequestHandler<GetAuthorPickerListQuery, Result<IReadOnlyList<ContentAuthorPickerItemDto>>>
{
    public Task<Result<IReadOnlyList<ContentAuthorPickerItemDto>>> Handle(
        GetAuthorPickerListQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => authors.GetPickerListAsync(request.Search, request.ActiveOnly, cancellationToken),
            ContentErrorCodes.AuthorNotFound);
}
