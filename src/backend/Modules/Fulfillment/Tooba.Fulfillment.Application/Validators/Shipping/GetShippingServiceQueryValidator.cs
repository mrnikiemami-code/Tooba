using FluentValidation;
using Tooba.Fulfillment.Application.Queries.GetShippingService;

namespace Tooba.Fulfillment.Application.Validators.Shipping;

/// <summary>
/// Transport/input shape validation for <see cref="GetShippingServiceQuery"/>.
/// Existence and read semantics stay in Application/Domain.
/// </summary>
public sealed class GetShippingServiceQueryValidator : AbstractValidator<GetShippingServiceQuery>
{
    /// <summary>Registers primitive-shape rules for the get shipping service query.</summary>
    public GetShippingServiceQueryValidator()
    {
        FulfillmentFluentRules.RequireId(this, x => x.ServiceId, FulfillmentValidationCodes.ShippingServiceIdRequired);
    }
}
