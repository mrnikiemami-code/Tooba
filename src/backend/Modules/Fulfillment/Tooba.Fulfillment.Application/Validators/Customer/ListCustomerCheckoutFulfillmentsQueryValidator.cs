using FluentValidation;
using Tooba.Fulfillment.Application.Queries.ListCustomerCheckoutFulfillments;

namespace Tooba.Fulfillment.Application.Validators.Customer;

/// <summary>
/// Transport/input shape validation for <see cref="ListCustomerCheckoutFulfillmentsQuery"/>.
/// The route checkoutId is untrusted input, so only its non-empty shape is checked.
/// Ownership, existence and access stay in the customer authorizer and Application/Domain.
/// </summary>
public sealed class ListCustomerCheckoutFulfillmentsQueryValidator
    : AbstractValidator<ListCustomerCheckoutFulfillmentsQuery>
{
    /// <summary>Registers primitive-shape rules for the customer checkout query.</summary>
    public ListCustomerCheckoutFulfillmentsQueryValidator()
    {
        FulfillmentFluentRules.RequireId(this, x => x.CheckoutId, FulfillmentValidationCodes.CheckoutIdRequired);
    }
}
