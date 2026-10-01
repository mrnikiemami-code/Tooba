using FluentValidation;

namespace Tooba.Party.Application.Admin.Sellers.Queries;

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
            .WithErrorCode(PartyAdminSellersValidationCodes.GridRequestRequired);
    }
}
