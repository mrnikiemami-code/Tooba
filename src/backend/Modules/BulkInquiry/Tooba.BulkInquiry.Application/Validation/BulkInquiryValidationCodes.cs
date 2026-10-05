namespace Tooba.BulkInquiry.Application.Validation;

/// <summary>
/// Stable machine codes for BulkInquiry FluentValidation transport/input shape.
/// Codes are never localized identity and never encode business semantics.
/// Validation codes are not registered in the error catalog: the canonical validation
/// pipeline maps them through the foundation <c>validation.failed</c> descriptor.
/// </summary>
public static class BulkInquiryValidationCodes
{
    /// <summary>Request envelope must be supplied.</summary>
    public const string RequestRequired = "bulk_inquiry.validation.request_required";

    /// <summary>Product slug is required.</summary>
    public const string SlugRequired = "bulk_inquiry.validation.slug_required";

    /// <summary>Full name is required.</summary>
    public const string FullNameRequired = "bulk_inquiry.validation.full_name_required";

    /// <summary>Phone is required.</summary>
    public const string PhoneRequired = "bulk_inquiry.validation.phone_required";

    /// <summary>Address is required.</summary>
    public const string AddressRequired = "bulk_inquiry.validation.address_required";
}
