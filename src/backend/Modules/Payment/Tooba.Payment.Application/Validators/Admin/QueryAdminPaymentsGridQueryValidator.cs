using FluentValidation;
using Tooba.Payment.Application.Queries.QueryAdminPaymentsGrid;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Validators.Admin;

/// <summary>
/// Transport validation for <see cref="QueryAdminPaymentsGridQuery"/>.
/// Only the primitive grid envelope is shaped here. Field-name/operator/connector/sort whitelists,
/// search semantics and supply/reservation enrichment remain owned by the Payment admin grid
/// normalizer and its stable semantic grid error codes.
/// </summary>
public sealed class QueryAdminPaymentsGridQueryValidator : AbstractValidator<QueryAdminPaymentsGridQuery>
{
    /// <summary>Registers primitive-shape rules for the grid query.</summary>
    public QueryAdminPaymentsGridQueryValidator()
    {
        RuleFor(x => x.Input)
            .NotNull()
            .WithErrorCode(PaymentValidationCodes.GridInputRequired);

        When(x => x.Input is not null, () =>
        {
            RuleFor(x => x.Input.Filters)
                .NotNull()
                .WithErrorCode(PaymentValidationCodes.GridFiltersRequired);
            RuleFor(x => x.Input.SortField)
                .Must(value => !string.IsNullOrWhiteSpace(value))
                .WithErrorCode(PaymentValidationCodes.GridSortFieldShape);
            RuleFor(x => x.Input.SortDirection)
                .Must(value => !string.IsNullOrWhiteSpace(value))
                .WithErrorCode(PaymentValidationCodes.GridSortDirectionShape);
            RuleFor(x => x.Input.Page)
                .Must(page => page >= 1)
                .WithErrorCode(PaymentValidationCodes.GridPageMin);
            RuleFor(x => x.Input.PageSize)
                .Must(pageSize => pageSize >= 1)
                .WithErrorCode(PaymentValidationCodes.GridPageSizeMin);
        });
    }
}
