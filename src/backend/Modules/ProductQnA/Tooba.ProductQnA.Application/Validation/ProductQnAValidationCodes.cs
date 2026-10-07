namespace Tooba.ProductQnA.Application.Validation;

/// <summary>
/// Stable machine codes for ProductQnA FluentValidation transport/input shape.
/// Codes are never localized identity and never encode business semantics.
/// Validation codes are not registered in the error catalog: the canonical validation
/// pipeline maps them through the foundation <c>validation.failed</c> descriptor.
/// </summary>
public static class ProductQnAValidationCodes
{
    /// <summary>Actor user id missing for customer submit.</summary>
    public const string ActorRequired = "product_qna.validation.actor_required";

    /// <summary>Submit body missing.</summary>
    public const string BodyRequired = "product_qna.validation.body_required";

    /// <summary>Product id missing.</summary>
    public const string ProductRequired = "product_qna.validation.product_required";

    /// <summary>Question body missing.</summary>
    public const string QuestionBodyRequired = "product_qna.validation.question_body_required";

    /// <summary>Product slug missing for storefront read.</summary>
    public const string SlugRequired = "product_qna.validation.slug_required";

    /// <summary>Page number out of range.</summary>
    public const string PageInvalid = "product_qna.validation.page_invalid";

    /// <summary>Page size out of range.</summary>
    public const string PageSizeInvalid = "product_qna.validation.page_size_invalid";
}
