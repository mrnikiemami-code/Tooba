namespace Tooba.Media.Application.Assets.Validators;

/// <summary>
/// Stable machine-readable codes for Media FluentValidation transport-shape failures.
/// These are transport identity codes only; they are never localized and never classify business
/// state. They are deliberately NOT registered as error-catalog descriptors: the canonical
/// <c>ValidationBehavior</c> pipeline maps them through the foundation <c>validation.failed</c>
/// descriptor, matching the certified Localization/Content/Cart/Offer precedent.
/// </summary>
public static class MediaValidationCodes
{
    /// <summary>Upload payload must carry a readable content stream.</summary>
    public const string UploadContentRequired = "media.validation.upload_content_required";

    /// <summary>Upload payload must carry the original file name.</summary>
    public const string UploadOriginalFileNameRequired = "media.validation.upload_original_file_name_required";

    /// <summary>Upload payload must carry the declared content type.</summary>
    public const string UploadContentTypeRequired = "media.validation.upload_content_type_required";

    /// <summary>Library query page number must be at least 1.</summary>
    public const string PageOutOfRange = "media.validation.page_out_of_range";

    /// <summary>Library query page size must be between 1 and 100.</summary>
    public const string PageSizeOutOfRange = "media.validation.page_size_out_of_range";

    /// <summary>Asset identifier must be supplied in the request.</summary>
    public const string MediaAssetIdRequired = "media.validation.media_asset_id_required";
}
