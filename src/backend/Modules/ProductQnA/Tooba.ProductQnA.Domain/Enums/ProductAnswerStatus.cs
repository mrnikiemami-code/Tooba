namespace Tooba.ProductQnA.Domain.Enums;

/// <summary>وضعیت پاسخ پرسش؛ فقط Published در PDP نمایش داده می‌شود.</summary>
public enum ProductAnswerStatus
{
    /// <summary>در انتظار تصمیم مدیر.</summary>
    Pending = 0,

    /// <summary>منتشرشده برای نمایش عمومی.</summary>
    Published = 1,
}
