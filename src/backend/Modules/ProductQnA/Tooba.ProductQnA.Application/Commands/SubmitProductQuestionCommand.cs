using FluentValidation;
using MediatR;
using Tooba.ProductQnA.Application;

namespace Tooba.ProductQnA.Application.Commands;

/// <summary>ثبت پرسش محصول برای Actor نشست.</summary>
public sealed record SubmitProductQuestionCommand(Guid ActorUserId, SubmitProductQuestion Body) : IRequest<Guid>;

/// <summary>Handler ثبت پرسش.</summary>
public sealed class SubmitProductQuestionCommandHandler(IProductQaDirectory directory)
    : IRequestHandler<SubmitProductQuestionCommand, Guid>
{
    /// <inheritdoc />
    public Task<Guid> Handle(SubmitProductQuestionCommand request, CancellationToken cancellationToken)
        => directory.SubmitQuestionAsync(request.ActorUserId, request.Body, cancellationToken);
}

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
