using FluentValidation;
using Tooba.Fulfillment.Application.Queries.GetAdminFulfillment;

namespace Tooba.Fulfillment.Application.Validators.Admin;

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
