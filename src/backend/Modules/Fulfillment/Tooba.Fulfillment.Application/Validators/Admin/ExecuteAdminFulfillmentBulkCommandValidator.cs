using FluentValidation;
using Tooba.Fulfillment.Application.Commands.ExecuteAdminFulfillmentBulk;

namespace Tooba.Fulfillment.Application.Validators.Admin;

/// <summary>
/// Transport/input shape validation for <see cref="ExecuteAdminFulfillmentBulkCommand"/>.
/// Only the request envelope null-shape is checked. Action-code allowlist, empty bulk semantics,
/// cross-seller rules, row identity matching, action compatibility and shipment resolution remain
/// Application/business behavior. ActorUserId is authorizer-derived.
/// </summary>
public sealed class ExecuteAdminFulfillmentBulkCommandValidator
    : AbstractValidator<ExecuteAdminFulfillmentBulkCommand>
{
    /// <summary>Registers primitive-shape rules for the admin bulk command.</summary>
    public ExecuteAdminFulfillmentBulkCommandValidator()
    {
        FulfillmentFluentRules.RequireReference(this, x => x.Request, FulfillmentValidationCodes.BulkRequestRequired);
    }
}
