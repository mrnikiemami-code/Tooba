namespace Tooba.Media.Domain.Enums;

/// <summary>وضعیت پردازش دارایی رسانه.</summary>
public enum MediaAssetStatus
{
    /// <summary>باینری و فراداده آمادهٔ ارائه است.</summary>
    Ready = 0,

    /// <summary>آپلود یا ذخیره‌سازی ناموفق بوده است.</summary>
    Failed = 1,
}
