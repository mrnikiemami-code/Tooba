namespace Tooba.BuildingBlocks;

/// <summary>
/// Technical HTTP mapping seam for the Host. Not a business exception taxonomy.
/// </summary>
public sealed class PlatformHttpException : Exception
{
    public PlatformHttpException(int statusCode, string title, string? errorCode = null)
        : base(title)
    {
        StatusCode = statusCode;
        Title = title;
        ErrorCode = errorCode;
    }

    public int StatusCode { get; }
    public string Title { get; }
    public string? ErrorCode { get; }
}
