using FluentValidation;
using Tooba.Payment.Application.Queries.GetAdminPayment;
using Tooba.Payment.Application.Validators;

namespace Tooba.Payment.Application.Validators.Admin;

/// <summary>
/// Transport validation for <see cref="GetAdminPaymentQuery"/>.
/// Admin authorization and payment existence stay in the endpoint authorizer and Application/Domain.
/// </summary>
public sealed class GetAdminPaymentQueryValidator : AbstractValidator<GetAdminPaymentQuery>
{
    /// <summary>Registers primitive-shape rules for the query.</summary>
    public GetAdminPaymentQueryValidator()
    {
        PaymentFluentRules.RequireId(this, x => x.PaymentId, PaymentValidationCodes.PaymentIdRequired);
    }
}
