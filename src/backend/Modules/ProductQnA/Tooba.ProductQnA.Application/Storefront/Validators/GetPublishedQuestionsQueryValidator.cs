using FluentValidation;
using Tooba.ProductQnA.Application.Storefront.Queries;
using Tooba.ProductQnA.Contracts.Errors;

namespace Tooba.ProductQnA.Application.Storefront.Validators;

/// <summary>VALIDATOR_REQUIRED — envelope ورودی خواندن عمومی.</summary>
public sealed class GetPublishedQuestionsQueryValidator : AbstractValidator<GetPublishedQuestionsQuery>
{
    /// <summary>قواعد slug/page/pageSize.</summary>
    public GetPublishedQuestionsQueryValidator()
    {
        RuleFor(x => x.ProductSlug).NotEmpty().WithErrorCode(ProductQnAErrorCodes.SlugRequired);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode(ProductQnAErrorCodes.PageInvalid);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithErrorCode(ProductQnAErrorCodes.PageSizeInvalid);
    }
}
