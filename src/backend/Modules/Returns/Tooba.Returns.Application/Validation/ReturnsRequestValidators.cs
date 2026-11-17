using FluentValidation;
using Tooba.BuildingBlocks.Grid;
using Tooba.Returns.Application.ReturnRequests.Commands;
using Tooba.Returns.Application.ReturnRequests.Queries;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Domain.ValueObjects;

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
        // TB-272 (B): one line per order line. Duplicate detection is list SHAPE (an item identified twice), not a
        // quantity/eligibility rule — the remaining-quantity authority stays in the evaluator and the Directory.
        RuleFor(x => x.Items)
            .Must(items => items!.Select(x => x.OrderLineId).Distinct().Count() == items.Count)
            .When(x => x.Items is { Count: > 0 })
            .WithErrorCode(ReturnsValidationCodes.DuplicateOrderLine);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.OrderLineId)
                .NotEqual(Guid.Empty)
                .WithErrorCode(ReturnsValidationCodes.OrderLineIdRequired);
            item.RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithErrorCode(ReturnsValidationCodes.QuantityInvalid)
                // TB-272-R1: the persisted quantity is numeric(18,6); a value with more fractional digits would be
                // stored as a different number of physical units. Refuse it here (endpoint-reachable) instead of
                // silently rounding it; the owning aggregate repeats the same invariant.
                .Must(PersistedReturnQuantity.IsExactlyRepresentable)
                .WithErrorCode(ReturnsValidationCodes.QuantityPrecision);
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

/// <summary>
/// TB-273: transport shape of the explicit recovery request. The kind must be a known intent and a money-related
/// intent must name the exact stored attempt the caller reviewed — the business binding (which attempt belongs to the
/// case, and whether it is still the single pending one) stays in the owning Directory.
/// </summary>
public sealed class RecoverReturnCommandValidator : AbstractValidator<RecoverReturnCommand>
{
    public RecoverReturnCommandValidator()
    {
        RuleFor(x => x.ReturnRequestId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(ReturnsValidationCodes.ReturnRequestIdRequired);
        RuleFor(x => x.Kind)
            .IsInEnum()
            .WithErrorCode(ReturnsValidationCodes.RecoveryKindInvalid);
        RuleFor(x => x.ExpectedRefundAttemptId)
            .NotNull()
            .WithErrorCode(ReturnsValidationCodes.RecoveryAttemptRequired)
            .When(x => x.Kind == ReturnRecoveryKind.ResumePendingRefund);
        RuleFor(x => x.ExpectedRefundAttemptId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(ReturnsValidationCodes.RecoveryAttemptRequired)
            .When(x => x.ExpectedRefundAttemptId.HasValue);
    }
}

/// <summary>
/// TB-277: transport shape of the explicit FIRST-refund request. Only the case identity travels; every amount,
/// payment, currency, destination and customer fact is read from the stored case by the owning directory.
/// </summary>
public sealed class StartReturnRefundCommandValidator : AbstractValidator<StartReturnRefundCommand>
{
    public StartReturnRefundCommandValidator()
    {
        RuleFor(x => x.ReturnRequestId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(ReturnsValidationCodes.ReturnRequestIdRequired);
    }
}

/// <summary>
/// TB-274: transport shape of a customer receipt. The occurrence identity, the non-empty line set, the positive and
/// exactly representable quantities, the defined condition and the no-duplicate-(item, condition) rule are shape; the
/// owning case/state/over-receipt/gating rules stay in the Directory.
/// </summary>
public sealed class RecordReturnReceiptCommandValidator : AbstractValidator<RecordReturnReceiptCommand>
{
    public RecordReturnReceiptCommandValidator()
    {
        RuleFor(x => x.ReturnRequestId)
            .NotEqual(Guid.Empty)
            .WithErrorCode(ReturnsValidationCodes.ReturnRequestIdRequired);
        RuleFor(x => x.OccurrenceKey)
            .NotEmpty()
            .WithErrorCode(ReturnsValidationCodes.ReceiptOccurrenceRequired)
            .MaximumLength(128)
            .WithErrorCode(ReturnsValidationCodes.ReceiptOccurrenceTooLong);
        RuleFor(x => x.Items)
            .NotNull()
            .WithErrorCode(ReturnsValidationCodes.ReceiptLinesEmpty)
            .Must(items => items is { Count: > 0 })
            .WithErrorCode(ReturnsValidationCodes.ReceiptLinesEmpty);
        RuleFor(x => x.Items)
            .Must(items => items!
                .Select(x => (x.ReturnItemId, x.Condition))
                .Distinct()
                .Count() == items.Count)
            .When(x => x.Items is { Count: > 0 })
            .WithErrorCode(ReturnsValidationCodes.ReceiptDuplicateLine);
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.ReturnItemId)
                .NotEqual(Guid.Empty)
                .WithErrorCode(ReturnsValidationCodes.ReceiptItemIdRequired);
            item.RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithErrorCode(ReturnsValidationCodes.ReceiptQuantityInvalid)
                .Must(PersistedReturnQuantity.IsExactlyRepresentable)
                .WithErrorCode(ReturnsValidationCodes.ReceiptQuantityInvalid);
            item.RuleFor(x => x.Condition)
                .IsInEnum()
                .WithErrorCode(ReturnsValidationCodes.ReceiptConditionInvalid);
        });
    }
}
