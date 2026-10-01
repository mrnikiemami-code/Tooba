using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Host.Admin.Development;

/// <summary>
/// GET /v1/admin/dev-context — مالک Host/Admin/Development.
/// HOST_DEVELOPMENT_PRESENTATION_CQRS_EXCEPTION (بدون request-handler ماژول).
/// </summary>
public static class AdminDevContextEndpoints
{
    /// <summary>کد پایدار نبودن مسیر Development.</summary>
    public const string UnavailableCode = "admin.dev.unavailable";

    /// <summary>مسیر dev-context مدیر را ثبت می‌کند.</summary>
    public static void MapAdminDevContextEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapGet("/v1/admin/dev-context", GetDevContext);
    }

    private static IResult GetDevContext(
        IHostEnvironment environment,
        ApiResponseFactory api)
    {
        if (!environment.IsDevelopment() || AdminDevActorBootstrap.Snapshot is not { } snapshot)
        {
            return api.From(Result.Failure<AdminDevContextResponse>(new SemanticError(UnavailableCode)));
        }

        return api.From(Result.Success(new AdminDevContextResponse(
            snapshot.ActorUserId,
            snapshot.ActorLabel,
            snapshot.TenantId)));
    }
}

/// <summary>شکل پاسخ موفق GET /v1/admin/dev-context.</summary>
public sealed record AdminDevContextResponse(Guid ActorUserId, string ActorLabel, string TenantId);
