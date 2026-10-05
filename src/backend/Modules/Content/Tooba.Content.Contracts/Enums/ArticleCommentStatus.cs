namespace Tooba.Content.Contracts.Enums;

/// <summary>وضعیت تعدیل نظر مقاله — قرارداد مرزی مشترک Content/Domain/Application/Endpoints.</summary>
public enum ArticleCommentStatus
{
    /// <summary>در انتظار بررسی.</summary>
    Pending = 0,
    /// <summary>تأییدشده و قابل نمایش عمومی (در صورت وجود سطح عمومی).</summary>
    Approved = 1,
    /// <summary>ردشده.</summary>
    Rejected = 2,
    /// <summary>پنهان اداری بدون حذف تاریخچه.</summary>
    Hidden = 3,
}
