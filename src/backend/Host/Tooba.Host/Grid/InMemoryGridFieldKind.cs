namespace Tooba.Host.Grid;

/// <summary>نوع فیلد برای اعمال فیلتر/مرتب‌سازی in-memory گرید Admin.</summary>
public enum InMemoryGridFieldKind
{
    /// <summary>فیلد متنی.</summary>
    Text,

    /// <summary>فیلد عددی.</summary>
    Number,

    /// <summary>فیلد تاریخ.</summary>
    Date,

    /// <summary>فیلد enum/status.</summary>
    Enum,
}
