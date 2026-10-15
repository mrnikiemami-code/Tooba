using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Contracts.Errors;

namespace Tooba.Fulfillment.Application.Shipping.Commands;

/// <summary>
/// فعال/غیرفعال‌سازی یک روش ارسال یک‌سطحی گزینه‌دار.
/// </summary>
/// <remarks>
/// <para>
/// ورودی فقط کد روش انتخابی در UI و وضعیت درخواستی است. هیچ سطح دومی از کلاینت پذیرفته نمی‌شود و
/// پیوند قدیمی (سرویس/گزینه) صرفاً از روی ردیف روش و در سمت سرور حل می‌شود.
/// </para>
/// <para>
/// فعال‌سازی و غیرفعال‌سازی دو عملیات متمایز روی همان فرمان‌اند: وضعیت صریح درخواست می‌شود و هیچ
/// به‌روزرسانی گروهی وجود ندارد. در شکست، هیچ نوشتنی رخ نمی‌دهد.
/// </para>
/// </remarks>
public sealed record SetOneLevelShippingMethodActivationCommand(string MethodCode, bool IsActive)
    : IRequest<Result<OneLevelShippingMethodActivationDto>>;

/// <summary>وضعیت فعال‌بودن یک روش یک‌سطحی پس از عملیات.</summary>
/// <param name="MethodCode">کد canonical روش؛ تغییرناپذیر.</param>
/// <param name="IsActive">وضعیت فعال‌بودن مؤثر.</param>
/// <param name="Changed">آیا این فراخوانی وضعیت را تغییر داد (نبودِ تغییر، idempotent است).</param>
public sealed record OneLevelShippingMethodActivationDto(string MethodCode, bool IsActive, bool Changed);

/// <summary>Handler فعال‌سازی روش یک‌سطحی — فقط از مسیر باریکِ گزینه‌دار و بدون هیچ تغییر هویتی.</summary>
public sealed class SetOneLevelShippingMethodActivationHandler
    : IRequestHandler<SetOneLevelShippingMethodActivationCommand, Result<OneLevelShippingMethodActivationDto>>
{
    private readonly IShippingOneLevelCatalogAdminActivation _activation;

    /// <summary>Handler را به مرجع فعال‌سازی وصل می‌کند.</summary>
    public SetOneLevelShippingMethodActivationHandler(IShippingOneLevelCatalogAdminActivation activation) =>
        _activation = activation;

    /// <inheritdoc />
    public async Task<Result<OneLevelShippingMethodActivationDto>> Handle(
        SetOneLevelShippingMethodActivationCommand request,
        CancellationToken cancellationToken)
    {
        var outcome = await _activation.SetActivationAsync(
            request.MethodCode, request.IsActive, cancellationToken);

        if (outcome.Status is ShippingOneLevelMethodActivationStatus.Applied
            or ShippingOneLevelMethodActivationStatus.NoChange)
        {
            return Result.Success(new OneLevelShippingMethodActivationDto(
                outcome.MethodCode,
                outcome.EffectiveIsActive,
                outcome.Status == ShippingOneLevelMethodActivationStatus.Applied));
        }

        // کدهای خطای Contracts در این تسک قابل افزودن نیستند (مسیر مجاز نیست)؛ بنابراین فقط کدهای
        // موجود و پایدار به‌کار می‌روند: «سرویس والد پیدا نشد»، «این عملیات برای این روش پشتیبانی
        // نمی‌شود» و «رد صریح». هیچ‌کدام هرگز به‌صورت ۵۰۰ یا موفقیت جعلی ظاهر نمی‌شوند.
        var code = outcome.Status switch
        {
            ShippingOneLevelMethodActivationStatus.InvalidMethodCode => FulfillmentErrorCodes.ShippingMethodCodeInvalid,
            ShippingOneLevelMethodActivationStatus.MethodNotFound => FulfillmentErrorCodes.ShippingMethodNotFound,
            ShippingOneLevelMethodActivationStatus.LegacyBindingMissing => FulfillmentErrorCodes.ShippingServiceNotFound,
            ShippingOneLevelMethodActivationStatus.LegacyOptionMissing => FulfillmentErrorCodes.ShippingMethodUnsupported,
            ShippingOneLevelMethodActivationStatus.LegacyParentInactive => FulfillmentErrorCodes.ShippingMethodUnsupported,
            ShippingOneLevelMethodActivationStatus.OptionlessRequiresServiceDecision
                => FulfillmentErrorCodes.ShippingMethodUnsupported,
            // دروازهٔ خاموش و کاتالوگ مهاجرت‌نشده: رد صریح و بدون نوشتن.
            _ => FulfillmentErrorCodes.Rejected,
        };
        return ShippingServiceSemantic.Failure<OneLevelShippingMethodActivationDto>(code);
    }
}
