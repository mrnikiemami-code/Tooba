using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Errors;

namespace Tooba.Shipping.Infrastructure.Rates;

/// <summary>
/// اعتبارسنجی هویت روش ارسال روی درز فقط-خواندنی Fulfillment Contracts
/// (<see cref="IShippingCatalogReader"/>). هیچ DbContext و هیچ جدول ماژول دیگری اینجا خوانده
/// نمی‌شود و هیچ نام گزینه‌ای هاردکد نیست.
/// </summary>
/// <remarks>
/// <b>قاعدهٔ یکسان‌سازی.</b> کد خدمت مسیر ممکن است به‌شکل متعارف درآید، ولی کد گزینه هرگز از
/// داخل کد خدمت جدا نمی‌شود: «tipax:express» یک کد خدمت نامعتبر است، نه خدمت tipax با گزینهٔ
/// express. اگر فراخوان مفهوم ترکیبی را می‌خواهد باید خودش آن را به دو مقدار جدا بشکند.
/// </remarks>
/// <remarks>
/// TB-SHIPPING-NEW-METHOD-ACTIVATION-170: روش تازهٔ canonical هیچ ردیف گزینهٔ قدیمی ندارد، پس
/// کلید نرخ مالک آن (سرویس + کد گزینهٔ پیوند) با فهرست گزینهٔ قدیمی قابل تأیید نبود. در این حالت
/// هویت از درز فقط-خواندنی <see cref="IShippingOneLevelCanonicalMethodReader"/> تأیید می‌شود؛
/// کد ساختگی همچنان رد می‌شود و هیچ ردیف قدیمی ساخته نمی‌شود.
/// </remarks>
public sealed class FulfillmentShippingMethodOptionValidator(
    IShippingCatalogReader catalog,
    IShippingOneLevelCanonicalMethodReader? canonicalMethods = null)
    : IShippingMethodOptionValidator
{
    /// <inheritdoc />
    public async Task<Result<bool>> ValidateAsync(
        string serviceCode,
        string? optionCode,
        CancellationToken cancellationToken)
    {
        var code = serviceCode?.Trim() ?? string.Empty;
        if (code.Length == 0)
        {
            return Result.Failure<bool>(new SemanticError(ShippingErrorCodes.ServiceCodeRequired));
        }

        var option = optionCode?.Trim();
        if (string.IsNullOrEmpty(option))
        {
            option = null;
        }

        var services = await catalog.ListAsync(cancellationToken);
        var service = services.FirstOrDefault(s =>
            string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase));

        // A service code that is not in the real catalog is refused: the rate policy key must be a
        // Fulfillment service code, never an invented one.
        if (service is null)
        {
            return Result.Failure<bool>(new SemanticError(ShippingErrorCodes.ServiceCodeUnknown));
        }

        if (option is null)
        {
            // A service-level policy is valid for the whole service.
            return Result.Success(true);
        }

        var optionExists = service.Options.Any(o =>
            string.Equals(o.Code, option, StringComparison.OrdinalIgnoreCase));
        if (optionExists)
        {
            return Result.Success(true);
        }

        // روش تازهٔ canonical: سرویس والد واقعی است اما گزینه فقط روی ردیف روش (بدون ردیف قدیمی)
        // نگاشت شده است. هویت از Contracts خوانده می‌شود؛ دروازهٔ خاموش یا کد ناشناخته ⇒ رد.
        if (canonicalMethods is not null
            && await canonicalMethods.IsCanonicalMethodBindingAsync(code, option, cancellationToken))
        {
            return Result.Success(true);
        }

        return Result.Failure<bool>(new SemanticError(ShippingErrorCodes.ServiceOptionUnknown));
    }
}
