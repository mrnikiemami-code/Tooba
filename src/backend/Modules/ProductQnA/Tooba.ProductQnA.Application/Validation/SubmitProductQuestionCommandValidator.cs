using FluentValidation;
using Tooba.ProductQnA.Application.Customer.Commands;

namespace Tooba.ProductQnA.Application.Validation;

/// <summary>اعتبارسنجی حمل‌ونقل ثبت پرسش.</summary>
public sealed class SubmitProductQuestionCommandValidator : AbstractValidator<SubmitProductQuestionCommand>
{
    /// <summary>قواعد حمل‌ونقل؛ قواعد دامنه در Domain می‌مانند.</summary>
    public SubmitProductQuestionCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty().WithErrorCode(ProductQnAValidationCodes.ActorRequired);
        RuleFor(x => x.Body).NotNull().WithErrorCode(ProductQnAValidationCodes.BodyRequired);
        RuleFor(x => x.Body.ProductId).NotEmpty().WithErrorCode(ProductQnAValidationCodes.ProductRequired);
        RuleFor(x => x.Body.Body).NotEmpty().WithErrorCode(ProductQnAValidationCodes.QuestionBodyRequired);
    }
}
