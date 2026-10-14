using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Contracts.Admin;

namespace Tooba.Shipping.Application.Rates.Ports;

/// <summary>
/// درز اعتبارسنجی دامنهٔ مقصد پیش از نوشتن نرخ/پوشش. مالکیت هویت مکان با Geography است؛ این درز فقط
/// می‌پرسد «آیا این شناسه‌ها یک مقصد واقعی، فعال و منسجم‌اند؟» و هیچ شناسه‌ای نمی‌سازد.
/// </summary>
public interface IShippingDestinationScopeValidator
{
    /// <summary>
    /// دامنهٔ درخواستی را با فهرست مکان‌نگاری مرجع می‌سنجد. شکست یعنی شناسه ناشناخته/غیرفعال/ناهم‌بسته
    /// است و نوشتن باید رد شود.
    /// </summary>
    /// <param name="scope">دامنهٔ مقصد ارسالی کلاینت.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    Task<Result<ShippingDestinationScopeWrite>> ValidateAsync(
        ShippingDestinationScopeWrite scope,
        CancellationToken cancellationToken);
}
