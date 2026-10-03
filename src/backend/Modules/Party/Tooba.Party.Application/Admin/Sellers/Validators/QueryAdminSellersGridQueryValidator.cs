using FluentValidation;
using Tooba.Party.Application.Admin.Sellers.Queries;
using Tooba.Party.Contracts.Errors;

namespace Tooba.Party.Application.Admin.Sellers.Validators;

/// <summary>
/// VALIDATOR_REQUIRED — فقط envelope ورودی؛ whitelist فیلد/عملگر در
/// <c>PartyAdminSellersGridPolicies</c> می‌ماند و اینجا تکرار نمی‌شود.
/// </summary>
public sealed class QueryAdminSellersGridQueryValidator : AbstractValidator<QueryAdminSellersGridQuery>
{
    /// <summary>ثبت قانون null نبودن Request.</summary>
    public QueryAdminSellersGridQueryValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .WithErrorCode(PartyErrorCodes.AdminSellersGridRequestRequired);
    }
}
