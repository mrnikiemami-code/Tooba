using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Content.Application.Categories.Models;
using Tooba.Content.Application.Categories.Ports;
using Tooba.Content.Contracts.Errors;
using Tooba.Content.Application.Composition;

namespace Tooba.Content.Application.Categories.Queries;

public sealed record GetCategoryTreeQuery(string LanguageCode, string? Search)
    : IRequest<Result<IReadOnlyList<ContentCategoryTreeNodeDto>>>;

public sealed class GetCategoryTreeQueryHandler(IContentCategoryDirectory categories)
    : IRequestHandler<GetCategoryTreeQuery, Result<IReadOnlyList<ContentCategoryTreeNodeDto>>>
{
    public Task<Result<IReadOnlyList<ContentCategoryTreeNodeDto>>> Handle(
        GetCategoryTreeQuery request, CancellationToken cancellationToken) =>
        ContentOperation.ExecuteAsync(
            () => categories.GetTreeAsync(request.LanguageCode, request.Search, cancellationToken),
            ContentErrorCodes.CategoryNotFound);
}
