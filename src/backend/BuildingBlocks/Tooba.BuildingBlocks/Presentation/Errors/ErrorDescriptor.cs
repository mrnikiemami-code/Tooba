namespace Tooba.BuildingBlocks.Presentation.Errors;

/// <summary>
/// توصیف صریح و پایدار خطا — بدون استنتاج HTTP از نام کد.
/// </summary>
/// <param name="Code">کد پایدار معنایی.</param>
/// <param name="Classification">طبقه‌بندی.</param>
/// <param name="HttpStatus">وضعیت HTTP صریح.</param>
/// <param name="LocalizationKey">کلید منبع محلی‌سازی.</param>
/// <param name="Severity">شدت لاگ پیشنهادی.</param>
/// <param name="SafeTitleFallback">عنوان امن انگلیسی اگر منبع موجود نباشد.</param>
public sealed record ErrorDescriptor(
    string Code,
    ErrorClassification Classification,
    int HttpStatus,
    string LocalizationKey,
    ErrorSeverity Severity,
    string SafeTitleFallback);

/// <summary>مشارکت‌کنندهٔ کاتالوگ خطای ماژول/foundation.</summary>
public interface IErrorCatalogContributor
{
    /// <summary>توصیف‌گرهای تحت مالکیت این مشارکت‌کننده.</summary>
    IReadOnlyList<ErrorDescriptor> Contribute();
}

/// <summary>کاتالوگ ترکیب‌شده و تغییرناپذیر پس از ساخت.</summary>
public interface IErrorDefinitionCatalog
{
    /// <summary>جستجوی توصیف‌گر؛ false اگر ثبت نشده باشد.</summary>
    bool TryGet(string code, out ErrorDescriptor descriptor);

    /// <summary>همهٔ کدهای ثبت‌شده.</summary>
    IReadOnlyCollection<string> RegisteredCodes { get; }
}

/// <summary>کاتالوگ مرکزی از مشارکت‌کنندگان؛ تکرار کد = شکست فوری.</summary>
public sealed class ErrorDefinitionCatalog : IErrorDefinitionCatalog
{
    private readonly IReadOnlyDictionary<string, ErrorDescriptor> _byCode;

    /// <summary>کاتالوگ را از مشارکت‌کنندگان می‌سازد و تکرار را رد می‌کند.</summary>
    public ErrorDefinitionCatalog(IEnumerable<IErrorCatalogContributor> contributors)
    {
        ArgumentNullException.ThrowIfNull(contributors);
        var map = new Dictionary<string, ErrorDescriptor>(StringComparer.OrdinalIgnoreCase);
        foreach (var contributor in contributors)
        {
            foreach (var descriptor in contributor.Contribute())
            {
                ArgumentNullException.ThrowIfNull(descriptor);
                if (string.IsNullOrWhiteSpace(descriptor.Code))
                {
                    throw new InvalidOperationException("error_descriptor_code_required");
                }

                var code = descriptor.Code.Trim();
                if (!map.TryAdd(code, descriptor with { Code = code }))
                {
                    throw new InvalidOperationException($"duplicate_error_descriptor:{code}");
                }
            }
        }

        _byCode = map;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> RegisteredCodes => _byCode.Keys.ToArray();

    /// <inheritdoc />
    public bool TryGet(string code, out ErrorDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            descriptor = null!;
            return false;
        }

        return _byCode.TryGetValue(code.Trim(), out descriptor!);
    }
}
