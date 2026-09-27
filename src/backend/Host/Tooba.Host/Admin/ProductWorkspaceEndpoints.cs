using Tooba.BuildingBlocks;

namespace Tooba.Host.Admin;

/// <summary>
/// مسیرهای HTTP ترکیب Workspace محصول. SQL بین‌ماژولی اینجا نوشته نمی‌شود.
/// W19: aggregate GET evacuated to ProductWorkspace.Endpoints.
/// W26: lifecycle POSTs evacuated to ProductWorkspace.Endpoints.
/// W27: variant create/patch evacuated to ProductWorkspace.Endpoints.
/// W28: product DELETE evacuated to Catalog.Endpoints.
/// W29: create / catalog-title / core / quantity-policy evacuated to ProductWorkspace.Endpoints.
/// W30: category / additional categories / brand evacuated to ProductWorkspace.Endpoints.
/// W31: list + grid query evacuated to ProductWorkspace.Endpoints (Host maps = 0).
/// Shell retained until W32 Host ProductWorkspace* deletion.
/// </summary>
public static class ProductWorkspaceEndpoints
{
    /// <summary>
    /// مسیرهای Admin Product Workspace را ثبت می‌کند (W31: no remaining Host maps).
    /// </summary>
    public static void MapProductWorkspaceEndpoints(this WebApplication app)
    {
        // W31: list/grid owned by ProductWorkspace.Endpoints. Keep registration no-op until W32.
        _ = app;
    }
}
