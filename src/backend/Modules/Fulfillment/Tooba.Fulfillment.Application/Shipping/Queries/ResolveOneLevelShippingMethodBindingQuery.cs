using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Contracts.Shipping;

namespace Tooba.Fulfillment.Application.Shipping.Queries;

/// <summary>
/// پیوند قطعی یک روش یک‌سطحی به سرویس/گزینهٔ قدیمی، همان‌طور که UI برای فراخوانی APIهای
/// موجود Shipping لازم دارد.
/// </summary>
/// <remarks>
/// <para>
/// این DTO عمداً فقط یک سطح را حمل می‌کند: هویت canonical روش و یک پیوند (serviceCode، optionCode).
/// هیچ ساختار تودرتوی provider→option و هیچ فهرست گزینه‌ای در آن نیست، بنابراین نمی‌تواند به
/// «سطح دوم پیکربندی فروشنده» تبدیل شود.
/// </para>
/// <para>
/// <paramref name="Resolved"/> تنها وقتی <c>true</c> است که دروازه روشن، کاتالوگ آماده و کد روش
/// canonical و موجود باشد. در غیر این‌صورت هیچ پیوندی برگردانده نمی‌شود تا کلاینت نتواند با
/// کد خام خودش سرویس/گزینه بسازد.
/// </para>
/// </remarks>
/// <param name="Gate">وضعیت دروازهٔ مؤثر: <c>Enabled</c> یا <c>Disabled</c>.</param>
/// <param name="Ready">آیا کاتالوگ واقعاً خوانده شد (جدول مهاجرت‌شده و خواندنی).</param>
/// <param name="Resolved">آیا پیوند قطعی حل شد.</param>
/// <param name="MethodCode">کد روش canonical؛ در نبود حل، تهی.</param>
/// <param name="ServiceCode">کد سرویس قدیمی والد؛ فقط وقتی حل شده باشد.</param>
/// <param name="OptionCode">کد گزینهٔ قدیمی؛ تهی یعنی روش به سرویس بدون گزینه نگاشته شده است.</param>
/// <param name="IsActive">فعال بودن روش؛ روش غیرفعال پیوند قابل‌استفاده نمی‌دهد.</param>
public sealed record OneLevelShippingMethodBindingDto(
    string Gate,
    bool Ready,
    bool Resolved,
    string MethodCode,
    string? ServiceCode,
    string? OptionCode,
    bool IsActive);

/// <summary>
/// حل پیوند قدیمی برای یک کد روش یک‌سطحی. تنها ورودی، کد روش انتخاب‌شده در UI است.
/// </summary>
/// <remarks>
/// این پرس‌وجو هیچ‌گاه <c>serviceCode</c>/<c>optionCode</c> خام را از کلاینت نمی‌پذیرد و
/// <c>method_code</c> را در مرورگر به سرویس/گزینه نمی‌شکند؛ حل فقط سمت سرور و از هویت canonical
/// انجام می‌شود. کد نامعتبر یا ناشناخته ⇒ <c>Resolved=false</c>، بدون خطای ۵۰۰.
/// </remarks>
public sealed record ResolveOneLevelShippingMethodBindingQuery(string MethodCode)
    : IRequest<Result<OneLevelShippingMethodBindingDto>>;

/// <summary>Handler حل پیوند — فقط‌خواندنی و fail-closed.</summary>
public sealed class ResolveOneLevelShippingMethodBindingHandler
    : IRequestHandler<ResolveOneLevelShippingMethodBindingQuery, Result<OneLevelShippingMethodBindingDto>>
{
    private readonly IShippingOneLevelCatalogAdminRead _read;

    /// <summary>Handler را به درز خواندن ادمین وصل می‌کند.</summary>
    public ResolveOneLevelShippingMethodBindingHandler(IShippingOneLevelCatalogAdminRead read) => _read = read;

    /// <inheritdoc />
    public async Task<Result<OneLevelShippingMethodBindingDto>> Handle(
        ResolveOneLevelShippingMethodBindingQuery request,
        CancellationToken cancellationToken)
    {
        var resolved = await _read.ResolveBindingAsync(request.MethodCode, cancellationToken);
        var gate = resolved.Gate == ShippingOneLevelCatalogGate.Enabled ? "Enabled" : "Disabled";
        return Result.Success(new OneLevelShippingMethodBindingDto(
            gate,
            resolved.CatalogReady,
            resolved.Resolved,
            resolved.MethodCode,
            resolved.Resolved ? resolved.ServiceCode : null,
            resolved.Resolved ? resolved.OptionCode : null,
            resolved.IsActive));
    }
}
