namespace Tooba.Story.Domain.Enums;

/// <summary>وضعیت انتشار استوری (چرخهٔ عمر عمومی).</summary>
public enum StoryStatus
{
    /// <summary>پیش‌نویس و غیرقابل نمایش عمومی.</summary>
    Draft = 0,
    /// <summary>زمان‌بندی‌شده برای آینده.</summary>
    Scheduled = 1,
    /// <summary>فعال برای نمایش عمومی در بازهٔ زمانی.</summary>
    Active = 2,
    /// <summary>منقضی‌شده.</summary>
    Expired = 3,
    /// <summary>غیرفعال دستی.</summary>
    Disabled = 4,
}
