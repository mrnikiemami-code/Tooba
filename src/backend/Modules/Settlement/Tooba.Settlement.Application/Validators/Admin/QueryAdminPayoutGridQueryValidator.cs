using FluentValidation;
using Tooba.Settlement.Application.Queries.QueryAdminPayoutGrid;

namespace Tooba.Settlement.Application.Validators.Admin;

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
