namespace Tooba.Media.Contracts.Errors;

/// <summary>
/// Stable Media machine error codes owned by the Media module boundary.
/// The strings are emitted by Media Domain/Application/Infrastructure, resolved by the canonical
/// composed error catalog and consumed by foreign modules through the Media Contracts ports; they
/// must never be renamed or repurposed.
/// </summary>
public static class MediaErrorCodes
{
    private static readonly HashSet<string> KnownCodes = new(StringComparer.Ordinal)
    {
        UploadFailed,
        TypeUnsupported,
        TooLarge,
        StorageUnavailable,
        Missing,
        ValidationFailed,
    };

    /// <summary>
    /// True when <paramref name="code"/> is a stable code declared by this Media catalog.
    /// Used by the module composition seam so Media faults map to <c>Result</c> while codes owned by
    /// another module propagate untouched to the canonical global exception boundary.
    /// </summary>
    public static bool IsKnown(string? code) =>
        !string.IsNullOrWhiteSpace(code) && KnownCodes.Contains(code);

    /// <summary>Upload transport, persistence or size rejection failed. HTTP 400.</summary>
    public const string UploadFailed = "media.upload.failed";

    /// <summary>Content type is not allowed. HTTP 400.</summary>
    public const string TypeUnsupported = "media.type.unsupported";

    /// <summary>Payload exceeds configured size. HTTP 400.</summary>
    public const string TooLarge = "media.too_large";

    /// <summary>Object store unavailable or misconfigured. HTTP 503.</summary>
    public const string StorageUnavailable = "media.storage.unavailable";

    /// <summary>Asset not found or not Ready. HTTP 404.</summary>
    public const string Missing = "media.missing";

    /// <summary>
    /// Media-owned validation classification code used when Media raises a validation fault outside
    /// the FluentValidation transport pipeline. HTTP 400.
    /// </summary>
    public const string ValidationFailed = "media.validation.failed";
}
