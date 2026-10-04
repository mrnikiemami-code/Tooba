using FluentValidation;
using Tooba.Fulfillment.Application.Fulfillments.Queries;
using Tooba.Fulfillment.Application.Validators;

namespace Tooba.Fulfillment.Application.Fulfillments.Validators;

/// <summary>
/// Transport/input shape validation for <see cref="GetSellerFulfillmentQuery"/>.
/// SellerPartyId is authorizer-derived and must not be policed as untrusted input.
/// </summary>
public sealed class GetSellerFulfillmentQueryValidator : AbstractValidator<GetSellerFulfillmentQuery>
{
    /// <summary>Registers primitive-shape rules for the seller query.</summary>
    public GetSellerFulfillmentQueryValidator()
    {
        FulfillmentFluentRules.RequireId(this, x => x.FulfillmentId, FulfillmentValidationCodes.FulfillmentIdRequired);
    }
}
