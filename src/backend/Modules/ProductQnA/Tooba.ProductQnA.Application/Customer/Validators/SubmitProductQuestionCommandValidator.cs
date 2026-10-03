using FluentValidation;
using Tooba.ProductQnA.Application.Customer.Commands;

namespace Tooba.ProductQnA.Application.Customer.Validators;

/// <summary>اعتبارسنجی حمل‌ونقل ثبت پرسش.</summary>
public sealed class SubmitProductQuestionCommandValidator : AbstractValidator<SubmitProductQuestionCommand>
{
    /// <summary>قواعد حمل‌ونقل؛ قواعد دامنه در Domain می‌مانند.</summary>
    public SubmitProductQuestionCommandValidator()
    {
        RuleFor(x => x.ActorUserId).NotEmpty().WithErrorCode("product_qna.validation.actor_required");
        RuleFor(x => x.Body).NotNull().WithErrorCode("product_qna.validation.body_required");
        RuleFor(x => x.Body.ProductId).NotEmpty().WithErrorCode("product_qna.validation.product_required");
        RuleFor(x => x.Body.Body).NotEmpty().WithErrorCode("product_qna.validation.question_body_required");
    }
}
