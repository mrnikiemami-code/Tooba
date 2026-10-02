
namespace Tooba.Host.Configuration;

/// <summary>
/// اتصال واحد marketplace؛ lookup فروشگاه از Host انجام نمی‌شود.
/// </summary>
internal sealed class MarketplaceOptions
{
    /// <summary>
    /// کلید ConnectionReference پایگاه marketplace.
    /// </summary>
    public string ConnectionReference { get; set; } = "";
}
