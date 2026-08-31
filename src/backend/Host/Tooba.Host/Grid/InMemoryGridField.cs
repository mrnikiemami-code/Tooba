namespace Tooba.Host.Grid;

/// <summary>تعریف یک فیلد قابل جستجو/فیلتر/مرتب‌سازی برای موتور in-memory.</summary>
public sealed class InMemoryGridField<T>
{
    /// <summary>نام فیلد canonical (همان columnId فرانت).</summary>
    public string Name { get; }

    /// <summary>خوانندهٔ مقدار از ردیف.</summary>
    public Func<T, object?> GetValue { get; }

    /// <summary>نوع فیلتر.</summary>
    public InMemoryGridFieldKind Kind { get; }

    /// <summary>در جستجٔ سراسری شرکت می‌کند.</summary>
    public bool Searchable { get; }

    /// <summary>مرتب‌سازی مجاز.</summary>
    public bool Sortable { get; }

    /// <summary>فیلتر مجاز.</summary>
    public bool Filterable { get; }

    /// <summary>سازندهٔ فیلد گرید in-memory.</summary>
    public InMemoryGridField(
        string name,
        Func<T, object?> getValue,
        InMemoryGridFieldKind kind,
        bool searchable = false,
        bool sortable = true,
        bool filterable = true)
    {
        Name = name;
        GetValue = getValue;
        Kind = kind;
        Searchable = searchable;
        Sortable = sortable;
        Filterable = filterable;
    }
}
