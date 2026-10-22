using FluentValidation;
using Tooba.Fulfillment.Application.ShippingTypes;
using Tooba.Fulfillment.Application.Validators;
using Tooba.Fulfillment.Domain.Aggregates;

namespace Tooba.Fulfillment.Application.ShippingTypes.Validators;

/// <summary>
/// اعتبارسنجی شکل transport/input برای <see cref="CreateShippingTypeCommand"/>.
/// </summary>
/// <remarks>
/// فقط شکل ورودی بررسی می‌شود: نبود مدل، کد خالی/بدشکل/بلند، ترتیب بیرون از بازه و ردیف ترجمهٔ null یا
/// خالی. وجود/فعال‌بودن زبان، یکتایی کد و قواعد دامنه در Application/Infrastructure می‌مانند.
/// </remarks>
public sealed class CreateShippingTypeCommandValidator : AbstractValidator<CreateShippingTypeCommand>
{
    /// <summary>قواعد شکل ورودی.</summary>
    public CreateShippingTypeCommandValidator()
    {
        RuleFor(x => x.Model)
            .NotNull()
            .WithErrorCode(FulfillmentValidationCodes.ShippingTypeModelRequired);

        When(x => x.Model is not null, () =>
        {
            RuleFor(x => x.Model.Code)
                .Must(code => ShippingType.TryNormalizeCode(code) is not null)
                .WithErrorCode(FulfillmentValidationCodes.ShippingTypeCodeShape);

            RuleFor(x => x.Model.SortOrder)
                .InclusiveBetween(ShippingType.SortOrderMinValue, ShippingType.SortOrderMaxValue)
                .WithErrorCode(FulfillmentValidationCodes.ShippingTypeSortOrderShape);

            RuleFor(x => x.Model.Translations)
                .NotNull()
                .WithErrorCode(FulfillmentValidationCodes.ShippingTypeTranslationsRequired);

            When(x => x.Model.Translations is not null, () =>
            {
                RuleForEach(x => x.Model.Translations)
                    .NotNull()
                    .WithErrorCode(FulfillmentValidationCodes.ShippingTypeTranslationShape);

                RuleForEach(x => x.Model.Translations)
                    .Must(entry => entry is not null
                                   && entry.LanguageId != Guid.Empty
                                   && !string.IsNullOrWhiteSpace(entry.Name)
                                   && entry.Name.Trim().Length <= ShippingType.NameMaxLength
                                   && (entry.Description is null
                                       || entry.Description.Trim().Length <= ShippingType.DescriptionMaxLength))
                    .WithErrorCode(FulfillmentValidationCodes.ShippingTypeTranslationShape);

                RuleFor(x => x.Model.Translations)
                    .Must(entries => entries
                        .Where(e => e is not null)
                        .Select(e => e.LanguageId)
                        .Distinct()
                        .Count() == entries.Count(e => e is not null))
                    .WithErrorCode(FulfillmentValidationCodes.ShippingTypeTranslationDuplicateLanguage);
            });
        });
    }
}

/// <summary>
/// اعتبارسنجی شکل transport/input برای <see cref="UpdateShippingTypeCommand"/>.
/// </summary>
/// <remarks>
/// حضور صریح <c>isActive</c> و <c>sortOrder</c> و <c>translations</c> در بدنه الزامی است؛ مقدار حذف‌شده یا
/// null نباید بی‌صدا به <c>false</c>/<c>0</c> تبدیل شود. شناسهٔ مسیر باید غیرتهی و بازبینی مثبت باشد.
/// </remarks>
public sealed class UpdateShippingTypeCommandValidator : AbstractValidator<UpdateShippingTypeCommand>
{
    /// <summary>قواعد شکل ورودی.</summary>
    public UpdateShippingTypeCommandValidator()
    {
        RuleFor(x => x.ShippingTypeId)
            .NotEmpty()
            .WithErrorCode(FulfillmentValidationCodes.ShippingTypeIdRequired);

        RuleFor(x => x.Model)
            .NotNull()
            .WithErrorCode(FulfillmentValidationCodes.ShippingTypeModelRequired);

        When(x => x.Model is not null, () =>
        {
            RuleFor(x => x.Model.ExpectedRevision)
                .GreaterThan(0)
                .WithErrorCode(FulfillmentValidationCodes.ShippingTypeRevisionShape);

            RuleFor(x => x.Model.SortOrder)
                .InclusiveBetween(ShippingType.SortOrderMinValue, ShippingType.SortOrderMaxValue)
                .WithErrorCode(FulfillmentValidationCodes.ShippingTypeSortOrderShape);

            RuleFor(x => x.Model.Translations)
                .NotNull()
                .WithErrorCode(FulfillmentValidationCodes.ShippingTypeTranslationsRequired);

            When(x => x.Model.Translations is not null, () =>
            {
                RuleForEach(x => x.Model.Translations)
                    .Must(entry => entry is not null
                                   && entry.LanguageId != Guid.Empty
                                   && !string.IsNullOrWhiteSpace(entry.Name)
                                   && entry.Name.Trim().Length <= ShippingType.NameMaxLength
                                   && (entry.Description is null
                                       || entry.Description.Trim().Length <= ShippingType.DescriptionMaxLength))
                    .WithErrorCode(FulfillmentValidationCodes.ShippingTypeTranslationShape);

                RuleFor(x => x.Model.Translations)
                    .Must(entries => entries
                        .Where(e => e is not null)
                        .Select(e => e.LanguageId)
                        .Distinct()
                        .Count() == entries.Count(e => e is not null))
                    .WithErrorCode(FulfillmentValidationCodes.ShippingTypeTranslationDuplicateLanguage);
            });
        });
    }
}
