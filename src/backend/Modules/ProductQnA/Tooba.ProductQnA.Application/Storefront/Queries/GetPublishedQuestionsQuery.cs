using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.ProductQnA.Application.Composition;
using Tooba.ProductQnA.Application.Models;
using Tooba.ProductQnA.Application.Ports;
using Tooba.ProductQnA.Contracts.Errors;

namespace Tooba.ProductQnA.Application.Storefront.Queries;

/// <summary>صفحهٔ عمومی پرسش‌های Published برای slug محصول.</summary>
public sealed record GetPublishedQuestionsQuery(string ProductSlug, int Page, int PageSize)
    : IRequest<Result<PublishedQaPage>>;

/// <summary>Handler خواندن عمومی.</summary>
public sealed class GetPublishedQuestionsQueryHandler(IProductQaDirectory directory)
    : IRequestHandler<GetPublishedQuestionsQuery, Result<PublishedQaPage>>
{
    /// <inheritdoc />
    public Task<Result<PublishedQaPage>> Handle(
        GetPublishedQuestionsQuery request,
        CancellationToken cancellationToken) =>
        ProductQnAOperation.ExecuteAsync(async () =>
        {
            var page = await directory.GetPublishedAsync(
                request.ProductSlug, request.Page, request.PageSize, cancellationToken);
            if (page is null)
                throw new SemanticException(new SemanticError(ProductQnAErrorCodes.NotFound));
            return page;
        });
}
