namespace Tooba.ProductQnA.Application.Models;

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
