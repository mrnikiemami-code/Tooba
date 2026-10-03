namespace Tooba.Media.Application.Assets.Models;

/// <summary>
/// Aggregate multi-file upload response shape preserved for Admin DAM clients.
/// Transport maps through <c>ApiResponseFactory.From</c> (HTTP 200 + raw JSON value).
/// </summary>
public sealed record MediaUploadBatchResponse(IReadOnlyList<object> Items);
