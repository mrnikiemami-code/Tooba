using FluentValidation;
using Tooba.ProductQnA.Application.Storefront.Queries;

namespace Tooba.ProductQnA.Application.Validation;

/// <summary>VALIDATOR_REQUIRED — envelope ورودی خواندن عمومی.</summary>
public sealed class GetPublishedQuestionsQueryValidator : AbstractValidator<GetPublishedQuestionsQuery>
{
    /// <summary>قواعد slug/page/pageSize.</summary>
    public GetPublishedQuestionsQueryValidator()
    {
        RuleFor(x => x.ProductSlug).NotEmpty().WithErrorCode(ProductQnAValidationCodes.SlugRequired);
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithErrorCode(ProductQnAValidationCodes.PageInvalid);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithErrorCode(ProductQnAValidationCodes.PageSizeInvalid);
    }
}
