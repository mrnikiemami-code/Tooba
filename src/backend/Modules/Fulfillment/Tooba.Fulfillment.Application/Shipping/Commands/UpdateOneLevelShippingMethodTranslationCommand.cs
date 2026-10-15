using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Localization.Contracts.Ports;

namespace Tooba.Fulfillment.Application.Shipping.Commands;

/// <summary>
/// نام نمایشی مستقل یک روش ارسال یک‌سطحی را در یک زبان می‌نویسد.
/// </summary>
/// <remarks>
/// <para>
/// تنها ورودی‌ها کد روش انتخاب‌شده در UI، زبان هدف و نام انسانی‌اند. هیچ
/// <c>serviceCode</c>/<c>optionCode</c> خام از کلاینت پذیرفته نمی‌شود، هیچ سطح دومی ساخته نمی‌شود و
/// کد روش هرگز تغییر نمی‌کند.
/// </para>
/// <para>
/// زبان با رجیستری زبان (همان قرارداد خواندن کاتالوگ) اعتبارسنجی می‌شود؛ نام خالی و نام بلندتر از
/// عرض ستون پایدار به‌صورت fail-closed رد می‌شوند و دروازهٔ خاموش هیچ نوشتنی نمی‌دهد.
/// </para>
/// </remarks>
public sealed record UpdateOneLevelShippingMethodTranslationCommand(
    string MethodCode,
    Guid LanguageId,
    string? Name)
    : IRequest<Result<OneLevelShippingMethodNameDto>>;

/// <summary>نام ذخیره‌شدهٔ یک روش در یک زبان.</summary>
/// <param name="MethodCode">کد canonical روش؛ تغییرناپذیر.</param>
/// <param name="LanguageId">زبان نوشته‌شده.</param>
/// <param name="Name">نام ذخیره‌شده.</param>
public sealed record OneLevelShippingMethodNameDto(string MethodCode, Guid LanguageId, string Name);

/// <summary>Handler نوشتن نام روش — فقط از مسیر نوشتن باریک و بدون هیچ تغییر هویتی.</summary>
public sealed class UpdateOneLevelShippingMethodTranslationHandler
    : IRequestHandler<UpdateOneLevelShippingMethodTranslationCommand, Result<OneLevelShippingMethodNameDto>>
{
    private readonly IShippingOneLevelCatalogAdminWrite _write;
    private readonly ILanguageLookup _languages;

    /// <summary>Handler را به درز نوشتن و رجیستری زبان وصل می‌کند.</summary>
    public UpdateOneLevelShippingMethodTranslationHandler(
        IShippingOneLevelCatalogAdminWrite write,
        ILanguageLookup languages)
    {
        _write = write;
        _languages = languages;
    }

    /// <inheritdoc />
    public async Task<Result<OneLevelShippingMethodNameDto>> Handle(
        UpdateOneLevelShippingMethodTranslationCommand request,
        CancellationToken cancellationToken)
    {
        var languages = await _languages.ListAsync(cancellationToken);
        if (request.LanguageId == Guid.Empty
            || languages.All(x => x.LanguageId != request.LanguageId))
        {
            // زبان ناشناخته هرگز به نوشتن ترجمه تبدیل نمی‌شود.
            return ShippingServiceSemantic.Failure<OneLevelShippingMethodNameDto>(
                FulfillmentErrorCodes.ShippingServiceLanguageInvalid);
        }

        var outcome = await _write.UpdateMethodNameAsync(
            request.MethodCode, request.LanguageId, request.Name, cancellationToken);

        if (outcome.Status == ShippingOneLevelMethodNameWriteStatus.Updated)
        {
            return Result.Success(new OneLevelShippingMethodNameDto(
                outcome.MethodCode, outcome.LanguageId, outcome.Name));
        }

        var code = outcome.Status switch
        {
            ShippingOneLevelMethodNameWriteStatus.InvalidMethodCode => FulfillmentErrorCodes.ShippingMethodCodeInvalid,
            ShippingOneLevelMethodNameWriteStatus.MethodNotFound => FulfillmentErrorCodes.ShippingMethodNotFound,
            ShippingOneLevelMethodNameWriteStatus.NameRequired => FulfillmentErrorCodes.ShippingMethodNameRequired,
            // دروازهٔ خاموش، کاتالوگ مهاجرت‌نشده و نام بلندتر از عرض ستون: رد صریح و بدون نوشتن.
            _ => FulfillmentErrorCodes.Rejected,
        };
        return ShippingServiceSemantic.Failure<OneLevelShippingMethodNameDto>(code);
    }
}
