using FluentValidation;
using Tooba.Shipping.Application.Authoring.Commands;
using Tooba.Shipping.Application.Authoring.Queries;
using Tooba.Shipping.Application.Validation;

namespace Tooba.Shipping.Application.Authoring.Validation;

/// <summary>
/// اعتبارسنجی شکل خواندن وضعیت انتشار: شناسهٔ مسیر باید غیرخالی باشد.
/// </summary>
public sealed class GetShippingAuthoringPublicationQueryValidator
    : AbstractValidator<GetShippingAuthoringPublicationQuery>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public GetShippingAuthoringPublicationQueryValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);
    }
}

/// <summary>
/// اعتبارسنجی شکل انتشار: هویت مسیر، بدنهٔ حاضر، بازبینی مثبت و نسخهٔ انتخاب <b>حاضر</b> و غیرمنفی.
/// </summary>
/// <remarks>
/// <b>غیبت ≠ صفر.</b> <c>ExpectedSelectionVersion</c> عمداً nullable است و الزام حضور آن جدا از
/// «غیرمنفی بودن» سنجیده می‌شود، تا نبودِ کلید در JSON هرگز به نسخهٔ صفر (و یک انتشار ناخواسته) تبدیل
/// نشود. این لایه هیچ هویت مالک، Binding، ارز یا پرچم فعال‌سازی از بدنه نمی‌پذیرد.
/// </remarks>
public sealed class PublishShippingAuthoringDraftCommandValidator
    : AbstractValidator<PublishShippingAuthoringDraftCommand>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public PublishShippingAuthoringDraftCommandValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);

        RuleFor(x => x.Request)
            .NotNull()
            .WithErrorCode(ShippingValidationCodes.BodyInvalid);

        RuleFor(x => x.Request.ExpectedRevision)
            .NotNull()
            .WithErrorCode(ShippingAuthoringValidationCodes.ExpectedRevisionInvalid)
            .Must(revision => revision is null || revision.Value > 0)
            .WithErrorCode(ShippingAuthoringValidationCodes.ExpectedRevisionInvalid)
            .When(x => x.Request is not null);

        RuleFor(x => x.Request.ExpectedSelectionVersion)
            .NotNull()
            .WithErrorCode(ShippingAuthoringValidationCodes.ExpectedSelectionVersionInvalid)
            .Must(version => version is null || version.Value >= 0)
            .WithErrorCode(ShippingAuthoringValidationCodes.ExpectedSelectionVersionInvalid)
            .When(x => x.Request is not null);
    }
}

/// <summary>
/// اعتبارسنجی شکل برداشتن انتشار: هویت مسیر، بدنهٔ حاضر و نسخهٔ انتخاب حاضر و غیرمنفی.
/// </summary>
public sealed class WithdrawShippingAuthoringPublicationCommandValidator
    : AbstractValidator<WithdrawShippingAuthoringPublicationCommand>
{
    /// <summary>قواعد را می‌سازد.</summary>
    public WithdrawShippingAuthoringPublicationCommandValidator()
    {
        RuleFor(x => x.DraftId)
            .NotEmpty()
            .WithErrorCode(ShippingAuthoringValidationCodes.DraftIdRequired);

        RuleFor(x => x.Request)
            .NotNull()
            .WithErrorCode(ShippingValidationCodes.BodyInvalid);

        RuleFor(x => x.Request.ExpectedSelectionVersion)
            .NotNull()
            .WithErrorCode(ShippingAuthoringValidationCodes.ExpectedSelectionVersionInvalid)
            .Must(version => version is null || version.Value >= 0)
            .WithErrorCode(ShippingAuthoringValidationCodes.ExpectedSelectionVersionInvalid)
            .When(x => x.Request is not null);
    }
}
