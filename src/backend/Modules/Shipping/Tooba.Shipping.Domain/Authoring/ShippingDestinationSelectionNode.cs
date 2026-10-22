namespace Tooba.Shipping.Domain.Authoring;

/// <summary>
/// سطح هویتی که یک انتخاب جغرافیایی در آن متوقف می‌شود. گونهٔ گره به‌همراه شناسه، هویت است؛
/// برچسب ترجمه‌شده هرگز هویت نیست.
/// </summary>
public enum ShippingDestinationNodeKind
{
    /// <summary>کل کشور.</summary>
    Country = 0,

    /// <summary>ناحیهٔ اداری در هر عمقی از سلسله‌مراتب.</summary>
    AdministrativeArea = 1,

    /// <summary>شهر.</summary>
    City = 2,

    /// <summary>ناحیهٔ خدمت زیر یک شهر.</summary>
    ServiceArea = 3,
}

/// <summary>
/// یک گرهٔ هویتی مقصد: گونه و شناسهٔ پایدار. گونه جدا ذخیره می‌شود زیرا یک GUID می‌تواند در دو
/// گونهٔ متفاوت وجود داشته باشد و آن دو انتخاب، دو واقعیت متفاوت‌اند.
/// </summary>
/// <param name="Kind">گونهٔ گره.</param>
/// <param name="Id">شناسهٔ پایدار گره.</param>
public readonly record struct ShippingDestinationNode(ShippingDestinationNodeKind Kind, Guid Id)
{
    /// <summary>آیا این گره یک انتخاب قابل ذخیره است (گونهٔ شناخته‌شده و شناسهٔ غیرخالی).</summary>
    public bool IsValid => Enum.IsDefined(Kind) && Id != Guid.Empty;
}

/// <summary>
/// زنجیرهٔ اثبات‌شدهٔ یک انتخاب، از ریشهٔ کشور تا خود گرهٔ انتخاب‌شده. این نوع فقط در Application
/// و از فکت‌های معتبر Geography ساخته می‌شود و <b>هرگز</b> از GUID خام یک درخواست.
/// </summary>
/// <remarks>
/// قاعدهٔ ناسازگاری روی همین زنجیره‌ها اجرا می‌شود: یک انتخاب «جد» انتخاب دیگر است اگر زنجیرهٔ
/// کوتاه‌تر، پیشوند دقیق زنجیرهٔ بلندتر باشد. این تعریف به عمق سلسله‌مراتب وابسته نیست و با
/// برابری GUID یا پیشوند نام جایگزین نمی‌شود.
/// </remarks>
public sealed class ShippingDestinationSelectionPath
{
    private readonly ShippingDestinationNode[] _chain;

    private ShippingDestinationSelectionPath(ShippingDestinationNode[] chain)
    {
        _chain = chain;
        Selected = chain[^1];
    }

    /// <summary>گرهٔ انتخاب‌شده (آخر زنجیره).</summary>
    public ShippingDestinationNode Selected { get; }

    /// <summary>زنجیرهٔ کامل از کشور تا گرهٔ انتخاب‌شده؛ حداقل یک عنصر.</summary>
    public IReadOnlyList<ShippingDestinationNode> Chain => _chain;

    /// <summary>زنجیرهٔ اثبات‌شده را می‌سازد.</summary>
    /// <param name="chain">کشور تا گرهٔ انتخاب‌شده.</param>
    public static ShippingDestinationSelectionPath FromChain(IReadOnlyList<ShippingDestinationNode> chain)
    {
        ArgumentNullException.ThrowIfNull(chain);
        if (chain.Count == 0 || chain.Any(node => !node.IsValid))
        {
            throw new ArgumentException("A selection path requires a non-empty chain of valid nodes.", nameof(chain));
        }

        if (chain[0].Kind != ShippingDestinationNodeKind.Country)
        {
            throw new ArgumentException("A selection path must start at the country node.", nameof(chain));
        }

        // A chain that repeats a node (a cycle the reference data should never contain) is refused
        // instead of being treated as a valid ancestry.
        if (chain.Distinct().Count() != chain.Count)
        {
            throw new ArgumentException("A selection path must not repeat a node.", nameof(chain));
        }

        return new ShippingDestinationSelectionPath([.. chain]);
    }
}

/// <summary>
/// قاعدهٔ خالص و بدون‌وابستگیِ ناسازگاری انتخاب‌های جغرافیایی در یک پیش‌نویس.
/// </summary>
/// <remarks>
/// دو انتخاب ناسازگارند اگر یکی جدِ دیگری باشد یا هر دو یکی باشند؛ در هر دو حالت یکی از زنجیره‌ها
/// پیشوند دقیق دیگری است. انتخاب‌های پیش‌نویس/مالک دیگر هرگز وارد این محاسبه نمی‌شوند.
/// </remarks>
public static class ShippingDestinationSelectionOverlap
{
    /// <summary>
    /// نخستین جفت ناسازگار را برمی‌گرداند؛ <c>null</c> یعنی هیچ ناسازگاری‌ای نیست.
    /// </summary>
    /// <param name="paths">زنجیره‌های اثبات‌شدهٔ همهٔ گروه‌های همان پیش‌نویس.</param>
    public static (int First, int Second)? FindConflict(IReadOnlyList<ShippingDestinationSelectionPath> paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        for (var first = 0; first < paths.Count; first++)
        {
            for (var second = first + 1; second < paths.Count; second++)
            {
                if (IsAncestorOrSame(paths[first].Chain, paths[second].Chain))
                {
                    return (first, second);
                }
            }
        }

        return null;
    }

    /// <summary>آیا یکی از دو زنجیره پیشوند دقیق دیگری است (برابری یا جد‌بودن).</summary>
    /// <param name="left">زنجیرهٔ نخست.</param>
    /// <param name="right">زنجیرهٔ دوم.</param>
    public static bool IsAncestorOrSame(
        IReadOnlyList<ShippingDestinationNode> left,
        IReadOnlyList<ShippingDestinationNode> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        var shared = Math.Min(left.Count, right.Count);
        for (var index = 0; index < shared; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }

        return true;
    }
}
