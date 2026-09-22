using Tooba.Wallet.Application.Models;

namespace Tooba.Wallet.Application.Ports;

/// <summary>Application seam for Wallet admin demo-preview (Infrastructure implements).</summary>
public interface IWalletDemoPreviewPort
{
    /// <summary>Last published demo snapshot, or null when seed is not ready.</summary>
    WalletDemoPreviewDto? TryGetCurrent();
}
