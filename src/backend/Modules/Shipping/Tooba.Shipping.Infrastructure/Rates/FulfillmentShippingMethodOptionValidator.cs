using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Observability.Tracing;
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
/// <remarks>
/// <b>TB-SHIPPING-ADMIN-BACKEND-AMSC-187-W3-R1.</b> هر فراخوانی واقعی Fulfillment یک span فرزند
/// canonical (<c>module.Shipping.Fulfillment.*</c>) می‌گیرد: خواندن کاتالوگ همیشه، و خواندن
/// canonical فقط وقتی همان شاخهٔ fallback واقعاً اجرا شود. هیچ کد خدمت/گزینه/PII به‌عنوان tag
/// نوشته نمی‌شود؛ نتیجهٔ null/false یک lookup کامل‌شده است، نه خطای زیرساخت ساختگی.
/// </remarks>
public sealed class FulfillmentShippingMethodOptionValidator(
    IShippingCatalogReader catalog,
    IModuleCallTracer moduleCallTracer,
    IShippingOneLevelCanonicalMethodReader? canonicalMethods = null)
    : IShippingMethodOptionValidator
{
    private const string SourceModule = "Shipping";
    private const string TargetModule = "Fulfillment";
    private const string ListCatalogOperation = "ListShippingCatalog";
    private const string CanonicalBindingOperation = "ResolveCanonicalMethodBinding";

    /// <inheritdoc />
    public async Task<Result<bool>> ValidateAsync(
        string serviceCode,
        string? optionCode,
        CancellationToken cancellationToken)
    {
        var code = serviceCode?.Trim() ?? string.Empty;
        if (code.Length == 0)
        {
            // رد محلی: هیچ فراخوانی مقصدی رخ نمی‌دهد، پس هیچ span فراخوانی ماژولی هم ساخته نمی‌شود.
            return Result.Failure<bool>(new SemanticError(ShippingErrorCodes.ServiceCodeRequired));
        }

        var option = optionCode?.Trim();
        if (string.IsNullOrEmpty(option))
        {
            option = null;
        }

        var services = await CallFulfillmentAsync(
            ListCatalogOperation,
            () => catalog.ListAsync(cancellationToken));

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
        if (canonicalMethods is not null)
        {
            var canonicalBinding = await CallFulfillmentAsync(
                CanonicalBindingOperation,
                () => canonicalMethods.IsCanonicalMethodBindingAsync(code, option, cancellationToken));
            if (canonicalBinding)
            {
                return Result.Success(true);
            }
        }

        return Result.Failure<bool>(new SemanticError(ShippingErrorCodes.ServiceOptionUnknown));
    }

    /// <summary>
    /// یک فراخوانی واقعی Fulfillment را با span فرزند canonical می‌پوشاند. مقدار بازگشتی، آرگومان‌ها،
    /// ترتیب شاخه‌ها و انتشار استثنا/لغو عیناً حفظ می‌شود؛ تنها <em>نوع</em> استثنا ثبت می‌شود.
    /// </summary>
    private async Task<T> CallFulfillmentAsync<T>(string operation, Func<Task<T>> call)
    {
        using var moduleCall = moduleCallTracer.Begin(SourceModule, TargetModule, operation);
        try
        {
            var result = await call();
            moduleCall.SetOk();
            return result;
        }
        catch (Exception exception)
        {
            moduleCall.SetError(exception);
            throw;
        }
    }
}
