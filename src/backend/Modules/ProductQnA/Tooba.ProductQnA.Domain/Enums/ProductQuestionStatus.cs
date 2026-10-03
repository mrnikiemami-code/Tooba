namespace Tooba.ProductQnA.Domain.Enums;

/// <summary>وضعیت چرخهٔ پرسش محصول؛ فقط Published برای عموم قابل مشاهده است.</summary>
public enum ProductQuestionStatus
{
    /// <summary>در انتظار تصمیم مدیر.</summary>
    Pending = 0,

    /// <summary>منتشرشده برای نمایش عمومی.</summary>
    Published = 1,

    /// <summary>ردشده و غیرقابل نمایش عمومی.</summary>
    Rejected = 2,
}
