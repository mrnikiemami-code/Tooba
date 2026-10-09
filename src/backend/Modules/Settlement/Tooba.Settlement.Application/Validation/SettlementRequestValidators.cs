using FluentValidation;
using Tooba.Settlement.Application.Payouts.Commands;
using Tooba.Settlement.Application.Payouts.Queries;

namespace Tooba.Settlement.Application.Validation;

/// <summary>
/// Transport validation for <see cref="RequestSellerPayoutCommand"/>.
/// Only the untrusted request body shape is checked. <c>SellerPartyId</c> and <c>ActorUserId</c>
/// come from the trusted seller authorization boundary and are deliberately not validated here;
/// available balance, payout eligibility, seller existence, idempotency uniqueness and payout
/// state/business policy stay in Application/Domain.
/// </summary>
public sealed class RequestSellerPayoutCommandValidator : AbstractValidator<RequestSellerPayoutCommand>
{
    /// <summary>Registers primitive-shape rules for the untrusted payout body.</summary>
    public RequestSellerPayoutCommandValidator()
    {
        RuleFor(x => x.Amount)
            .Must(amount => amount > 0)
            .WithErrorCode(SettlementValidationCodes.PayoutAmountPositive);

        RuleFor(x => x.IdempotencyKey)
            .Must(key => !string.IsNullOrWhiteSpace(key))
            .WithErrorCode(SettlementValidationCodes.IdempotencyKeyShape);
    }
}

/// <summary>
/// Transport validation for <see cref="ProcessAdminPayoutCommand"/>.
/// Only the untrusted route identifier is checked. <c>ActorUserId</c> is supplied by the trusted
/// admin authorization boundary and is deliberately not validated here; payout existence, state
/// and eligibility stay in Application/Domain.
/// </summary>
public sealed class ProcessAdminPayoutCommandValidator : AbstractValidator<ProcessAdminPayoutCommand>
{
    /// <summary>Registers primitive-shape rules for the route identifier.</summary>
    public ProcessAdminPayoutCommandValidator()
    {
        RuleFor(x => x.PayoutRequestId)
            .NotEmpty()
            .WithErrorCode(SettlementValidationCodes.PayoutRequestIdRequired);
    }
}

/// <summary>
/// Transport validation for <see cref="RetryAdminPayoutCommand"/>.
/// Only the untrusted route identifier is checked. <c>ActorUserId</c> is supplied by the trusted
/// admin authorization boundary and is deliberately not validated here; retry eligibility, state
/// and business policy stay in Application/Domain.
/// </summary>
public sealed class RetryAdminPayoutCommandValidator : AbstractValidator<RetryAdminPayoutCommand>
{
    /// <summary>Registers primitive-shape rules for the route identifier.</summary>
    public RetryAdminPayoutCommandValidator()
    {
        RuleFor(x => x.PayoutRequestId)
            .NotEmpty()
            .WithErrorCode(SettlementValidationCodes.PayoutRequestIdRequired);
    }
}

/// <summary>
/// Transport validation for <see cref="QueryAdminPayoutGridQuery"/>.
/// Only the primitive request-envelope shape is checked. Field/operator/sort/connector whitelists,
/// paging normalization, filter semantics, advanced-filter connectors and search semantics remain
/// owned by the module-owned <c>AdminPayoutGridQueryPolicy</c> and are deliberately NOT duplicated.
/// </summary>
public sealed class QueryAdminPayoutGridQueryValidator : AbstractValidator<QueryAdminPayoutGridQuery>
{
    /// <summary>Registers the request-envelope shape rule.</summary>
    public QueryAdminPayoutGridQueryValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithErrorCode(SettlementValidationCodes.GridRequestRequired);
    }
}
