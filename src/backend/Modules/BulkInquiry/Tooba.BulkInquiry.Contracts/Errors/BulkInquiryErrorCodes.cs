namespace Tooba.BulkInquiry.Contracts.Errors;

/// <summary>Stable semantic error codes owned by BulkInquiry.</summary>
public static class BulkInquiryErrorCodes
{
    /// <summary>Bulk inquiry submission was rejected by domain/application rules.</summary>
    public const string Rejected = "bulk_inquiry.rejected";

    /// <summary>Request body is required.</summary>
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
