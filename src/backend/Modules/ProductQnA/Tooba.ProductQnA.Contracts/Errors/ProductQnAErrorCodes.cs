namespace Tooba.ProductQnA.Contracts.Errors;

/// <summary>Stable semantic error codes owned by ProductQnA.</summary>
public static class ProductQnAErrorCodes
{
    /// <summary>Question submission was rejected by domain/application rules.</summary>
    public const string Rejected = "product_qna.rejected";

    /// <summary>Shared foundation customer session required (do not re-register descriptor).</summary>
    public const string SessionRequired = "customer.session.required";
}
