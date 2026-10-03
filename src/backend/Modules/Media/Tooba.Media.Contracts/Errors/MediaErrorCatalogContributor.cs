using Microsoft.AspNetCore.Http;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.Media.Contracts.Ports;

namespace Tooba.Media.Contracts.Errors;

/// <summary>Explicit Media error catalog for SafeErrorMapper / ApiResponseFactory.</summary>
public sealed class MediaErrorCatalogContributor : IErrorCatalogContributor
{
    /// <inheritdoc />
    public IReadOnlyList<ErrorDescriptor> Contribute() =>
    [
        D(MediaErrorCodes.ValidationFailed, ErrorClassification.Validation, StatusCodes.Status400BadRequest,
            "Validation failed."),
        D(MediaErrorCodes.UploadFailed, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Media upload failed."),
        D(MediaErrorCodes.TypeUnsupported, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Media type is unsupported."),
        D(MediaErrorCodes.TooLarge, ErrorClassification.Business, StatusCodes.Status400BadRequest,
            "Media file is too large."),
        D(MediaErrorCodes.StorageUnavailable, ErrorClassification.Platform, StatusCodes.Status503ServiceUnavailable,
            "Media storage is unavailable."),
        D(MediaErrorCodes.Missing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Media was not found."),
        D(MediaAssetContractCodes.AssetMissing, ErrorClassification.NotFound, StatusCodes.Status404NotFound,
            "Media asset is missing."),
    ];

    private static ErrorDescriptor D(
        string code,
        ErrorClassification classification,
        int status,
        string fallback) =>
        new(code, classification, status, code, ErrorSeverity.Warning, fallback);
}
