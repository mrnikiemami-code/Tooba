using FluentValidation;
using Tooba.Fulfillment.Application.Queries.QueryAdminFulfillmentWorkQueue;

namespace Tooba.Fulfillment.Application.Validators.Admin;

/// <summary>
/// Transport/input shape validation for <see cref="QueryAdminFulfillmentWorkQueueQuery"/>.
/// Only the request envelope null-shape is checked. Paging normalization, field/operator allowlists,
/// sort normalization, search, filter and advanced-connector semantics stay in
/// <c>AdminFulfillmentGridQueryPolicy</c>.
/// </summary>
public sealed class QueryAdminFulfillmentWorkQueueQueryValidator
    : AbstractValidator<QueryAdminFulfillmentWorkQueueQuery>
{
    /// <summary>Registers primitive-shape rules for the admin work-queue query.</summary>
    public QueryAdminFulfillmentWorkQueueQueryValidator()
    {
        FulfillmentFluentRules.RequireReference(this, x => x.Request, FulfillmentValidationCodes.WorkQueueRequestRequired);
    }
}
