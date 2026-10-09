using Tooba.Support.Application.Tickets.Models;

namespace Tooba.Support.Application.Tickets.Ports;

/// <summary>Application seam for Support admin demo-preview (Infrastructure implements).</summary>
public interface ISupportDemoPreviewPort
{
    /// <summary>Last published demo snapshot, or null when seed is not ready.</summary>
    SupportDemoSnapshotDto? TryGetCurrent();
}
