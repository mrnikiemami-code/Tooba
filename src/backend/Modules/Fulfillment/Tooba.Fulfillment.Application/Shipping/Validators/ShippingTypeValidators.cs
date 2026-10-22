using FluentValidation;
using Tooba.Fulfillment.Application.Shipping.Models;
using Tooba.Fulfillment.Application.Shipping.Commands;
using Tooba.Fulfillment.Application.Shipping.Queries;
using Tooba.Fulfillment.Application.Validators;
using Tooba.Fulfillment.Domain.Aggregates;

namespace Tooba.Fulfillment.Application.Shipping.Validators;

/// <summary>
/// اعتبارسنجی حضور/شکل ورودی برای <see cref="CreateShippingTypeCommand"/>.
/// </summary>
/// <remarks>
/// کلید غایب و مقدار null یکسان نیستند با مقدار مجاز: <c>sortOrder</c> غایب/تهی و <c>code</c>
/// تهی/فاصله‌دار و <c>translations</c> غایب/تهی/حاوی عنصر null همه رد می‌شوند. یکتایی کد، وجود زبان و
/// قواعد دامنه در Application/Infrastructure می‌مانند.
/// </remarks>
public sealed class CreateShippingTypeCommandValidator : AbstractValidator<CreateShippingTypeCommand>
{
    /// <summary>قواعد حضور و شکل ورودی.</summary>
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
                .NotNull()
                .WithErrorCode(FulfillmentValidationCodes.ShippingTypeSortOrderShape)
                .Must(order => order is null
                               || (order.Value >= ShippingType.SortOrderMinValue
                                   && order.Value <= ShippingType.SortOrderMaxValue))
                .WithErrorCode(FulfillmentValidationCodes.ShippingTypeSortOrderShape);

            RuleFor(x => x.Model.Translations)
                .NotNull()
                .WithErrorCode(FulfillmentValidationCodes.ShippingTypeTranslationsRequired);

            ShippingTypeTranslationRules.Apply(this, x => x.Model.Translations);
        });
    }
}

/// <summary>
/// اعتبارسنجی حضور/شکل ورودی برای <see cref="UpdateShippingTypeCommand"/>.
/// </summary>
/// <remarks>
/// <c>isActive</c>، <c>sortOrder</c>، <c>expectedRevision</c> و <c>translations</c> باید صریحاً در بدنه
/// باشند؛ غایب یا null هرگز به <c>false</c>/<c>0</c> تبدیل نمی‌شود. شناسهٔ مسیر باید غیرتهی باشد.
/// </remarks>
public sealed class UpdateShippingTypeCommandValidator : AbstractValidator<UpdateShippingTypeCommand>
{
    /// <summary>قواعد حضور و شکل ورودی.</summary>
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
                .NotNull()
                .WithErrorCode(FulfillmentValidationCodes.ShippingTypeRevisionShape)
                .Must(revision => revision is null || revision.Value > 0)
                .WithErrorCode(FulfillmentValidationCodes.ShippingTypeRevisionShape);

            RuleFor(x => x.Model.IsActive)
                .NotNull()
                .WithErrorCode(FulfillmentValidationCodes.ShippingTypeActiveRequired);

            RuleFor(x => x.Model.SortOrder)
                .NotNull()
                .WithErrorCode(FulfillmentValidationCodes.ShippingTypeSortOrderShape)
                .Must(order => order is null
                               || (order.Value >= ShippingType.SortOrderMinValue
                                   && order.Value <= ShippingType.SortOrderMaxValue))
                .WithErrorCode(FulfillmentValidationCodes.ShippingTypeSortOrderShape);

            RuleFor(x => x.Model.Translations)
                .NotNull()
                .WithErrorCode(FulfillmentValidationCodes.ShippingTypeTranslationsRequired);

            ShippingTypeTranslationRules.Apply(this, x => x.Model.Translations);
        });
    }
}

/// <summary>اعتبارسنجی شناسهٔ مسیر جزئیات؛ شناسهٔ تهی هرگز به لایهٔ Application نمی‌رسد.</summary>
public sealed class GetShippingTypeQueryValidator : AbstractValidator<GetShippingTypeQuery>
{
    /// <summary>قواعد شناسه.</summary>
    public GetShippingTypeQueryValidator()
    {
        RuleFor(x => x.ShippingTypeId)
            .NotEmpty()
            .WithErrorCode(FulfillmentValidationCodes.ShippingTypeIdRequired);
    }
}

/// <summary>قواعد مشترک فهرست ترجمه‌ها؛ هیچ عنصر null نادیده گرفته نمی‌شود.</summary>
internal static class ShippingTypeTranslationRules
{
    internal static void Apply<T>(
        AbstractValidator<T> validator,
        System.Linq.Expressions.Expression<Func<T, IReadOnlyList<ShippingTypeTranslationInput?>?>> selector)
    {
        validator.RuleFor(selector)
            .Must(entries => entries is null || entries.All(entry => entry is not null))
            .WithErrorCode(FulfillmentValidationCodes.ShippingTypeTranslationShape);

        validator.RuleFor(selector)
            .Must(entries => entries is null
                             || entries.All(entry => entry is not null
                                                     && entry.LanguageId is { } id
                                                     && id != Guid.Empty
                                                     && !string.IsNullOrWhiteSpace(entry.Name)
                                                     && entry.Name!.Trim().Length <= ShippingType.NameMaxLength
                                                     && (entry.Description is null
                                                         || entry.Description.Trim().Length
                                                         <= ShippingType.DescriptionMaxLength)))
            .WithErrorCode(FulfillmentValidationCodes.ShippingTypeTranslationShape);

        validator.RuleFor(selector)
            .Must(entries => entries is null
                             || entries
                                 .Where(entry => entry is not null && entry.LanguageId is not null)
                                 .Select(entry => entry!.LanguageId)
                                 .Distinct()
                                 .Count() == entries.Count(entry => entry is not null && entry.LanguageId is not null))
            .WithErrorCode(FulfillmentValidationCodes.ShippingTypeTranslationDuplicateLanguage);
    }
}
