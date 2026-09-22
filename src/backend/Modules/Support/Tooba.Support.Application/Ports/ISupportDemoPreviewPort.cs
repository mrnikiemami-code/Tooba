using Tooba.Support.Application.Models;

namespace Tooba.Support.Application.Ports;

/// <summary>Application seam for Support admin demo-preview (Infrastructure implements).</summary>
public interface ISupportDemoPreviewPort
{
    /// <summary>Last published demo snapshot, or null when seed is not ready.</summary>
    SupportDemoSnapshotDto? TryGetCurrent();
}
