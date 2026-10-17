using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Shipping.Ports;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Localization.Contracts.Ports;

namespace Tooba.Fulfillment.Application.Shipping.Commands;

/// <summary>
/// یک روش ارسال مستقل canonical می‌سازد (هر روش = یک ردیف مدیریتی مستقل).
/// </summary>
/// <remarks>
/// <para>
/// ورودی‌ها فقط هویت/نمایش‌اند: کد canonical روش، metadata اختیاری ارائه‌دهنده و نام‌های زبان‌های
/// واقعی رجیستری. هیچ <c>serviceCode</c>/<c>optionCode</c>/<c>optionId</c> از کلاینت پذیرفته نمی‌شود؛
/// پیوند قدیمی توسط سرور از همان کد مشتق و اعتبارسنجی می‌شود.
/// </para>
/// <para>
/// روش تازه هرگز «فعال» ساخته نمی‌شود و هیچ سیاست مالی/نرخ/پوشش/برنامه‌ای جعل نمی‌شود؛ پس تا وقتی
/// سیاست اختصاصی همین روش ثبت نشود و درز فعال‌سازی ۰۹۶ آن را تأیید نکند، برای مشتری قابل‌انتخاب
/// نیست. زبان‌های ناشناخته و نام خالی/تکراری پیش از هر نوشتنی رد می‌شوند.
/// </para>
/// </remarks>
public sealed record CreateOneLevelShippingMethodCommand(
    string? MethodCode,
    string? ProviderKind,
    IReadOnlyList<OneLevelShippingMethodCreateName>? Translations)
    : IRequest<Result<OneLevelShippingMethodCreatedDto>>;

/// <summary>یک نام زبان در درخواست ایجاد روش.</summary>
public sealed record OneLevelShippingMethodCreateName(Guid LanguageId, string? Name);

/// <summary>روش canonical ساخته‌شده؛ همهٔ مقادیر از پاسخ authoritative سرور می‌آیند.</summary>
public sealed record OneLevelShippingMethodCreatedDto(
    string MethodCode,
    string ProviderKind,
    string LegacyServiceCode,
    string? LegacyOptionCode,
    bool IsActive,
    int SortOrder,
    IReadOnlyList<OneLevelShippingMethodCreatedTranslationDto> Translations);

/// <summary>ترجمهٔ نوشته‌شدهٔ روش تازه در یک زبان.</summary>
public sealed record OneLevelShippingMethodCreatedTranslationDto(Guid LanguageId, string Name);

/// <summary>Handler ایجاد روش — رجیستری زبان، سپس درز نوشتن مالک، بدون هیچ fallback.</summary>
public sealed class CreateOneLevelShippingMethodHandler
    : IRequestHandler<CreateOneLevelShippingMethodCommand, Result<OneLevelShippingMethodCreatedDto>>
{
    private readonly IShippingOneLevelCatalogAdminCreate _create;
    private readonly ILanguageLookup _languages;

    /// <summary>Handler را به درز ایجاد و رجیستری زبان وصل می‌کند.</summary>
    public CreateOneLevelShippingMethodHandler(
        IShippingOneLevelCatalogAdminCreate create,
        ILanguageLookup languages)
    {
        _create = create;
        _languages = languages;
    }

    /// <inheritdoc />
    public async Task<Result<OneLevelShippingMethodCreatedDto>> Handle(
        CreateOneLevelShippingMethodCommand request,
        CancellationToken cancellationToken)
    {
        var requested = request.Translations ?? [];
        if (requested.Count == 0)
        {
            return ShippingServiceSemantic.Failure<OneLevelShippingMethodCreatedDto>(
                FulfillmentErrorCodes.ShippingMethodNameRequired);
        }

        var languages = await _languages.ListAsync(cancellationToken);
        var known = languages.Select(x => x.LanguageId).ToHashSet();
        var seen = new HashSet<Guid>();
        var names = new List<ShippingOneLevelMethodCreateName>(requested.Count);

        foreach (var entry in requested)
        {
            if (entry.LanguageId == Guid.Empty || !known.Contains(entry.LanguageId))
            {
                // زبان ناشناخته هرگز به نوشتن ترجمه تبدیل نمی‌شود و هیچ زبان پیش‌فرضی جعل نمی‌شود.
                return ShippingServiceSemantic.Failure<OneLevelShippingMethodCreatedDto>(
                    FulfillmentErrorCodes.ShippingServiceLanguageInvalid);
            }

            if (!seen.Add(entry.LanguageId))
            {
                return ShippingServiceSemantic.Failure<OneLevelShippingMethodCreatedDto>(
                    FulfillmentErrorCodes.Rejected);
            }

            names.Add(new ShippingOneLevelMethodCreateName(entry.LanguageId, entry.Name ?? string.Empty));
        }

        var outcome = await _create.CreateMethodAsync(
            request.MethodCode ?? string.Empty,
            request.ProviderKind,
            names,
            cancellationToken);

        if (outcome.Status != ShippingOneLevelMethodCreateStatus.Created)
        {
            return ShippingServiceSemantic.Failure<OneLevelShippingMethodCreatedDto>(
                outcome.Status switch
                {
                    ShippingOneLevelMethodCreateStatus.InvalidMethodCode =>
                        FulfillmentErrorCodes.ShippingMethodCodeInvalid,
                    ShippingOneLevelMethodCreateStatus.MethodCodeDuplicate =>
                        FulfillmentErrorCodes.ShippingMethodCodeDuplicate,
                    ShippingOneLevelMethodCreateStatus.BindingDuplicate =>
                        FulfillmentErrorCodes.ShippingMethodBindingDuplicate,
                    ShippingOneLevelMethodCreateStatus.NameRequired =>
                        FulfillmentErrorCodes.ShippingMethodNameRequired,
                    // دروازهٔ خاموش، کاتالوگ مهاجرت‌نشده، نام بلند، زبان تکراری و برخورد هم‌زمان:
                    // رد صریح و بدون هیچ ادعای ایجاد.
                    _ => FulfillmentErrorCodes.Rejected,
                });
        }

        return Result.Success(new OneLevelShippingMethodCreatedDto(
            outcome.MethodCode,
            outcome.ProviderKind,
            outcome.LegacyServiceCode,
            outcome.LegacyOptionCode,
            outcome.IsActive,
            outcome.SortOrder,
            names
                .Select(x => new OneLevelShippingMethodCreatedTranslationDto(x.LanguageId, x.Name.Trim()))
                .ToArray()));
    }
}
