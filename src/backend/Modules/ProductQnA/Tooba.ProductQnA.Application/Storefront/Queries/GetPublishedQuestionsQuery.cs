using MediatR;
using Tooba.ProductQnA.Application.Models;
using Tooba.ProductQnA.Application.Ports;

namespace Tooba.ProductQnA.Application.Storefront.Queries;

/// <summary>صفحهٔ عمومی پرسش‌های Published برای slug محصول.</summary>
public sealed record GetPublishedQuestionsQuery(string ProductSlug, int Page, int PageSize)
    : IRequest<PublishedQaPage?>;

/// <summary>Handler خواندن عمومی.</summary>
public sealed class GetPublishedQuestionsQueryHandler(IProductQaDirectory directory)
    : IRequestHandler<GetPublishedQuestionsQuery, PublishedQaPage?>
{
    /// <inheritdoc />
    public Task<PublishedQaPage?> Handle(GetPublishedQuestionsQuery request, CancellationToken cancellationToken)
        => directory.GetPublishedAsync(request.ProductSlug, request.Page, request.PageSize, cancellationToken);
}
