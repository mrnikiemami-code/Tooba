namespace Tooba.Media.Contracts.Errors;

/// <summary>
/// Stable Media machine error codes (catalog-backed in W3).
/// Ad-hoc endpoint strings must converge here.
/// </summary>
public static class MediaErrorCodes
{
    /// <summary>Upload transport or persistence failed. HTTP 400/500.</summary>
    public const string UploadFailed = "media.upload.failed";

    /// <summary>Content type is not allowed. HTTP 400.</summary>
    public const string TypeUnsupported = "media.type.unsupported";

    /// <summary>Payload exceeds configured size. HTTP 400.</summary>
    public const string TooLarge = "media.too_large";

    /// <summary>Object store unavailable or misconfigured. HTTP 503/400.</summary>
    public const string StorageUnavailable = "media.storage.unavailable";

    /// <summary>Asset not found or not Ready. HTTP 404.</summary>
    public const string Missing = "media.missing";

    /// <summary>Transport validation failed. HTTP 400.</summary>
    public const string ValidationFailed = "media.validation.failed";
}
