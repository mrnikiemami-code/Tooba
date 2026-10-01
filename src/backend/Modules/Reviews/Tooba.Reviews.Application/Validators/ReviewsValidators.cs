using FluentValidation;
using Tooba.Reviews.Application.Commands;

namespace Tooba.Reviews.Application.Validators;

/// <summary>کدهای شکل انتقال Reviews.</summary>
public static class ReviewsValidationCodes
{
    public const string ProductIdRequired = "reviews.productId.required";
    public const string RatingRange = "reviews.rating.range";
    public const string BodyRequired = "reviews.body.required";
}

/// <summary>اعتبارسنجی ثبت نظر.</summary>
public sealed class SubmitProductReviewCommandValidator : AbstractValidator<SubmitProductReviewCommand>
{
    public SubmitProductReviewCommandValidator()
    {
        RuleFor(x => x.Input.ProductId)
            .NotEmpty()
            .WithErrorCode(ReviewsValidationCodes.ProductIdRequired);
        RuleFor(x => x.Input.Rating)
            .InclusiveBetween(1, 5)
            .WithErrorCode(ReviewsValidationCodes.RatingRange);
        RuleFor(x => x.Input.Body)
            .NotEmpty()
            .WithErrorCode(ReviewsValidationCodes.BodyRequired);
    }
}
