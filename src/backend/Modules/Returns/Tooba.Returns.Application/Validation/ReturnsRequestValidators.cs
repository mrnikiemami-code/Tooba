using FluentValidation;
using Tooba.BuildingBlocks.Grid;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Queries;
using Tooba.Returns.Application.ReturnRequests.Models;

namespace Tooba.Returns.Application.Validation;

#pragma warning disable CS1591

/// <summary>
/// Returns transport-shape validators (AMSC W1). Exactly the endpoint-reachable requests whose shape can
/// be malformed get a validator; the deliberately unvalidated requests are recorded in the W1 evidence
/// with a durable reason (route-constrained <c>:guid</c> identifier or server-derived actor). Only
/// transport shape is validated here — business/domain rules stay in the Returns Domain/Application.
/// </summary>
public sealed class CreateReturnCommandValidator : AbstractValidator<CreateReturnCommand>
{
    public CreateReturnCommandValidator()
    {
        RuleFor(x => x.SellerOrderId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(ReturnsValidationCodes.SellerOrderIdRequired);
        RuleFor(x => x.IdempotencyKey)
            .NotEmpty()
            .WithErrorCode(ReturnsValidationCodes.IdempotencyKeyRequired)
            .MaximumLength(128)
            .WithErrorCode(ReturnsValidationCodes.IdempotencyKeyTooLong);
        RuleFor(x => x.Reason)
            .MaximumLength(512)
            .WithErrorCode(ReturnsValidationCodes.ReasonTooLong);
        RuleFor(x => x.Items)
            .NotNull()
            .WithErrorCode(ReturnsValidationCodes.ItemsRequired)
            .Must(items => items is { Count: > 0 })
            .WithErrorCode(ReturnsValidationCodes.ItemsEmpty);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.OrderLineId)
                .NotEqual(Guid.Empty)
                .WithErrorCode(ReturnsValidationCodes.OrderLineIdRequired);
            item.RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithErrorCode(ReturnsValidationCodes.QuantityInvalid);
        });
        RuleFor(x => x.RefundDestination)
            .IsInEnum()
            .WithErrorCode(ReturnsValidationCodes.RefundDestinationInvalid);
    }
}

public sealed class ApproveReturnCommandValidator : AbstractValidator<ApproveReturnCommand>
{
    public ApproveReturnCommandValidator()
    {
        RuleFor(x => x.ReturnRequestId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(ReturnsValidationCodes.ReturnRequestIdRequired);
        RuleFor(x => x.RefundDestination)
            .IsInEnum()
            .When(x => x.RefundDestination.HasValue)
            .WithErrorCode(ReturnsValidationCodes.RefundDestinationInvalid);
    }
}

public sealed class RejectReturnCommandValidator : AbstractValidator<RejectReturnCommand>
{
    public RejectReturnCommandValidator()
    {
        RuleFor(x => x.ReturnRequestId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(ReturnsValidationCodes.ReturnRequestIdRequired);
        RuleFor(x => x.Reason)
            .MaximumLength(512)
            .WithErrorCode(ReturnsValidationCodes.ReasonTooLong);
    }
}

public sealed class QueryAdminReturnsGridQueryValidator : AbstractValidator<QueryAdminReturnsGridQuery>
{
    public QueryAdminReturnsGridQueryValidator()
        => RuleFor(x => x.Request)
            .NotNull()
            .WithErrorCode(ReturnsValidationCodes.GridRequestRequired);
}
