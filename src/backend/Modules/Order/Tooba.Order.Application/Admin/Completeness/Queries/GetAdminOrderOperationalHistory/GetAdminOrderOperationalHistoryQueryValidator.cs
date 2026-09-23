using FluentValidation;
using Tooba.Order.Application.Validation;

namespace Tooba.Order.Application.Admin.Completeness.Queries.GetAdminOrderOperationalHistory;

/// <summary>Paging and id primitives for operational history.</summary>
public sealed class GetAdminOrderOperationalHistoryQueryValidator
    : AbstractValidator<GetAdminOrderOperationalHistoryQuery>
{
    public GetAdminOrderOperationalHistoryQueryValidator()
    {
        RuleFor(x => x.CheckoutId)
            .NotEmpty()
            .WithErrorCode(OrderValidationCodes.CheckoutIdRequired);
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode(OrderValidationCodes.PageMin);
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .WithErrorCode(OrderValidationCodes.PageSizeRange);
    }
}
