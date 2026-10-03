namespace Tooba.ProductQnA.Contracts.Errors;

/// <summary>Stable semantic error codes owned by ProductQnA.</summary>
public static class ProductQnAErrorCodes
{
    /// <summary>Question submission was rejected by domain/application rules.</summary>
    public const string Rejected = "product_qna.rejected";

    /// <summary>Published questions for the product slug were not found.</summary>
    public const string NotFound = "product_qna.not_found";

    /// <summary>Shared foundation customer session required (do not re-register descriptor).</summary>
    public const string SessionRequired = "customer.session.required";

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
