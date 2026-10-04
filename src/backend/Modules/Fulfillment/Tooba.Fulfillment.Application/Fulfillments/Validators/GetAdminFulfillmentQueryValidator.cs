using FluentValidation;
using Tooba.Fulfillment.Application.Fulfillments.Queries;
using Tooba.Fulfillment.Application.Validators;

namespace Tooba.Fulfillment.Application.Fulfillments.Validators;

/// <summary>
/// Transport/input shape validation for <see cref="GetAdminFulfillmentQuery"/>.
/// Admin authorization and fulfillment existence stay in the endpoint authorizer and Application.
/// </summary>
public sealed class GetAdminFulfillmentQueryValidator : AbstractValidator<GetAdminFulfillmentQuery>
{
    /// <summary>Registers primitive-shape rules for the admin detail query.</summary>
    public GetAdminFulfillmentQueryValidator()
    {
        FulfillmentFluentRules.RequireId(this, x => x.FulfillmentId, FulfillmentValidationCodes.FulfillmentIdRequired);
    }
}
