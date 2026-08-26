namespace Tooba.ProductQnA.Application;

/// <summary>ورودی ثبت پرسش؛ هویت Actor و نام عمومی از سرور تأمین می‌شود.</summary>
public sealed record SubmitProductQuestion(Guid ProductId, string Body);

/// <summary>DTO عمومی و امن پرسش Published با پاسخ Published اختیاری.</summary>
public sealed record PublishedQaItem(
    Guid QuestionId,
    string AuthorDisplayName,
    string Body,
    DateTimeOffset CreatedAt,
    string? AnswerBody,
    string? AnswerAuthorDisplayName,
    DateTimeOffset? AnswerCreatedAt);

/// <summary>صفحهٔ عمومی پرسش‌های Published.</summary>
public sealed record PublishedQaPage(IReadOnlyList<PublishedQaItem> Items, int Page, int PageSize, long TotalCount);

/// <summary>قابلیت کاربردی ثبت و خواندن عمومی پرسش‌های محصول.</summary>
public interface IProductQaDirectory
{
    /// <summary>پرسش را برای Actor نشست ثبت می‌کند.</summary>
    Task<Guid> SubmitQuestionAsync(Guid actorUserId, SubmitProductQuestion request, CancellationToken cancellationToken);

    /// <summary>صفحهٔ Published محصول را با slug برمی‌گرداند.</summary>
    Task<PublishedQaPage?> GetPublishedAsync(string productSlug, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>شمارش Published برای نشان PDP.</summary>
    Task<long> CountPublishedAsync(Guid productId, CancellationToken cancellationToken);

    /// <summary>پرسش و پاسخ Published را برای دانهٔ توسعه منتشر می‌کند.</summary>
    Task PublishQuestionWithAnswerAsync(
        Guid productId,
        Guid authorUserId,
        string authorDisplayName,
        string questionBody,
        string answerAuthorDisplayName,
        string answerBody,
        CancellationToken cancellationToken);
}
