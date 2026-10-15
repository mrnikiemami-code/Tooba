using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Localization.Contracts.Ports;

namespace Tooba.Fulfillment.Application.Shipping.Queries;

/// <summary>
/// یک روش یک‌سطحی در فهرست ادمین. عمداً فقط هویت پایدار، برچسب، فعال‌بودن و ترتیب را حمل
/// می‌کند: نه <c>provider_kind</c> و نه پیوند قدیمی، تا هرگز «سطح دوم» پیکربندی ساخته نشود.
/// </summary>
public sealed record OneLevelShippingMethodListItemDto(
    string MethodCode,
    string Name,
    bool IsActive,
    int SortOrder);

/// <summary>
/// وضعیت مؤثر کاتالوگ یک‌سطحی برای پنل ادمین. <paramref name="Gate"/> و
/// <paramref name="Ready"/> صریح‌اند تا فرانت fail-closed بماند و «خالی» با «آماده» اشتباه نشود.
/// </summary>
/// <param name="Gate">وضعیت دروازهٔ مؤثر: <c>Enabled</c> یا <c>Disabled</c>.</param>
/// <param name="Ready">آیا کاتالوگ واقعاً خوانده شد (جدول مهاجرت‌شده و خواندنی).</param>
/// <param name="Methods">همهٔ روش‌ها شامل غیرفعال‌ها؛ ترتیب پایدار.</param>
public sealed record OneLevelShippingCatalogAdminDto(
    string Gate,
    bool Ready,
    IReadOnlyList<OneLevelShippingMethodListItemDto> Methods);

/// <summary>
/// خواندن فقط-خواندنی وضعیت و فهرست کاتالوگ یک‌سطحی برای پنل ادمین.
/// ورودی تنها یک زبان نمایشی اختیاری است؛ بنابراین هیچ شکل انتقالی نامعتبری وجود ندارد.
/// </summary>
public sealed record ListOneLevelShippingMethodsQuery(string? Language)
    : IRequest<Result<OneLevelShippingCatalogAdminDto>>;

/// <summary>Handler فهرست ادمین کاتالوگ یک‌سطحی — فقط‌خواندنی، بدون هیچ نوشتن یا fallback.</summary>
public sealed class ListOneLevelShippingMethodsHandler
    : IRequestHandler<ListOneLevelShippingMethodsQuery, Result<OneLevelShippingCatalogAdminDto>>
{
    private readonly IShippingOneLevelCatalogAdminRead _read;
    private readonly ILanguageLookup _languages;

    /// <summary>Handler را می‌سازد.</summary>
    public ListOneLevelShippingMethodsHandler(IShippingOneLevelCatalogAdminRead read, ILanguageLookup languages)
    {
        _read = read;
        _languages = languages;
    }

    /// <inheritdoc />
    public async Task<Result<OneLevelShippingCatalogAdminDto>> Handle(
        ListOneLevelShippingMethodsQuery request,
        CancellationToken cancellationToken)
    {
        var langId = await ShippingServiceSemantic.ResolveLanguageIdAsync(_languages, request.Language, cancellationToken);
        var read = await _read.ReadAsync(cancellationToken);

        var items = read.Methods
            .Select(m =>
            {
                var picked = m.Translations.FirstOrDefault(t => t.LanguageId == langId)
                    ?? m.Translations.FirstOrDefault();
                // برچسب فقط از دادهٔ واقعی می‌آید؛ در نبود ترجمه خالی می‌ماند و نام اختراع نمی‌شود.
                return new OneLevelShippingMethodListItemDto(
                    m.MethodCode,
                    picked?.Name?.Trim() ?? string.Empty,
                    m.IsActive,
                    m.SortOrder);
            })
            .ToList();

        var gate = read.Gate == ShippingOneLevelCatalogGate.Enabled ? "Enabled" : "Disabled";
        return Result.Success(new OneLevelShippingCatalogAdminDto(gate, read.CatalogReady, items));
    }
}
