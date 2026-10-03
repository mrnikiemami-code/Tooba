using FluentValidation;
using Tooba.Media.Application.Assets.Queries;
using Tooba.Media.Contracts.Errors;

namespace Tooba.Media.Application.Assets.Validators;

/// <summary>Transport-shape validation for <see cref="QueryMediaAssetsQuery"/>.</summary>
public sealed class QueryMediaAssetsQueryValidator : AbstractValidator<QueryMediaAssetsQuery>
{
    /// <summary>Registers paging bounds.</summary>
    public QueryMediaAssetsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode(MediaErrorCodes.ValidationFailed);
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithErrorCode(MediaErrorCodes.ValidationFailed);
    }
}
