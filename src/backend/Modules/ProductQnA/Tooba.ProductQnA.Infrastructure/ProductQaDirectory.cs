using Microsoft.EntityFrameworkCore;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;
using Tooba.ProductQnA.Application;
using Tooba.ProductQnA.Domain;
using Tooba.ProductQnA.Infrastructure.Persistence;

namespace Tooba.ProductQnA.Infrastructure;

/// <summary>دایرکتوری ProductQnA با خواندن فقط از قرارداد Catalog و schema خودش.</summary>
public sealed class ProductQaDirectory : IProductQaDirectory
{
    private readonly ProductQnADbContext _db;
    private readonly ICatalogLookupGateway _catalog;

    /// <summary>وابستگی‌های مالک را تزریق می‌کند.</summary>
    public ProductQaDirectory(ProductQnADbContext db, ICatalogLookupGateway catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<Guid> SubmitQuestionAsync(Guid actorUserId, SubmitProductQuestion request, CancellationToken cancellationToken)
    {
        var product = await _catalog.FindReviewableProductByIdAsync(request.ProductId, cancellationToken);
        if (product is null || product.Status != CatalogPublicationStatus.Published)
            throw new InvalidOperationException("محصول منتشرشده پیدا نشد.");

        var now = DateTimeOffset.UtcNow;
        var question = ProductQuestion.Create(product.ProductId, actorUserId, "مشتری توبا", request.Body, now);
        _db.Questions.Add(question);
        await _db.SaveChangesAsync(cancellationToken);
        return question.QuestionId;
    }

    /// <inheritdoc />
    public async Task<PublishedQaPage?> GetPublishedAsync(string productSlug, int page, int pageSize, CancellationToken cancellationToken)
    {
        var product = await _catalog.FindReviewableProductBySlugAsync(productSlug, cancellationToken);
        if (product is null || product.Status != CatalogPublicationStatus.Published) return null;

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Questions.AsNoTracking()
            .Where(x => x.ProductId == product.ProductId && x.Status == ProductQuestionStatus.Published);
        var totalCount = await query.LongCountAsync(cancellationToken);
        var questions = await query
            .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.QuestionId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(cancellationToken);

        var questionIds = questions.Select(x => x.QuestionId).ToArray();
        var answers = await _db.Answers.AsNoTracking()
            .Where(x => questionIds.Contains(x.QuestionId) && x.Status == ProductAnswerStatus.Published)
            .ToDictionaryAsync(x => x.QuestionId, cancellationToken);

        var items = questions.Select(q =>
        {
            var answer = answers.GetValueOrDefault(q.QuestionId);
            return new PublishedQaItem(
                q.QuestionId, q.AuthorDisplayName, q.Body, q.CreatedAt,
                answer?.Body, answer?.AuthorDisplayName, answer?.CreatedAt);
        }).ToList();

        return new PublishedQaPage(items, page, pageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<long> CountPublishedAsync(Guid productId, CancellationToken cancellationToken) =>
        await _db.Questions.AsNoTracking()
            .LongCountAsync(x => x.ProductId == productId && x.Status == ProductQuestionStatus.Published, cancellationToken);

    /// <inheritdoc />
    public async Task PublishQuestionWithAnswerAsync(
        Guid productId,
        Guid authorUserId,
        string authorDisplayName,
        string questionBody,
        string answerAuthorDisplayName,
        string answerBody,
        CancellationToken cancellationToken)
    {
        var product = await _catalog.FindReviewableProductByIdAsync(productId, cancellationToken);
        if (product is null || product.Status != CatalogPublicationStatus.Published)
            throw new InvalidOperationException("محصول منتشرشده پیدا نشد.");

        var now = DateTimeOffset.UtcNow;
        var moderator = Guid.Parse("12000000-0000-4000-8000-000000000099");
        var question = ProductQuestion.Create(productId, authorUserId, authorDisplayName, questionBody, now);
        question.Publish(moderator, now);
        _db.Questions.Add(question);

        var answer = ProductAnswer.Create(question.QuestionId, answerAuthorDisplayName, answerBody, now);
        answer.Publish();
        _db.Answers.Add(answer);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
