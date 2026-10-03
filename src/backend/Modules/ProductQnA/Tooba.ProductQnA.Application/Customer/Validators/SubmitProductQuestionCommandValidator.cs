using FluentValidation;
using Tooba.ProductQnA.Application.Customer.Commands;
using Tooba.ProductQnA.Contracts.Errors;

namespace Tooba.ProductQnA.Application.Customer.Validators;

/// <summary>اعتبارسنجی حمل‌ونقل ثبت پرسش.</summary>
public sealed class SubmitProductQuestionCommandValidator : AbstractValidator<SubmitProductQuestionCommand>
{
    /// <summary>قواعد حمل‌ونقل؛ قواعد دامنه در Domain می‌مانند.</summary>
    public SubmitProductQuestionCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty().WithErrorCode(ProductQnAErrorCodes.ActorRequired);
        RuleFor(x => x.Body).NotNull().WithErrorCode(ProductQnAErrorCodes.BodyRequired);
        RuleFor(x => x.Body.ProductId).NotEmpty().WithErrorCode(ProductQnAErrorCodes.ProductRequired);
        RuleFor(x => x.Body.Body).NotEmpty().WithErrorCode(ProductQnAErrorCodes.QuestionBodyRequired);
    }
}
