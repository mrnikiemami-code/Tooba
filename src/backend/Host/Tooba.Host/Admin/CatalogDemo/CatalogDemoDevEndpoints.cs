using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;

namespace Tooba.Host.Admin.CatalogDemo;

/// <summary>
/// مسیرهای Development/Testing برای reset+seed و وضعیت Catalog Demo.
/// </summary>
public static class CatalogDemoDevEndpoints
{
    /// <summary>ثبت مسیرهای demo catalog.</summary>
    public static void MapCatalogDemoDevEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/admin/catalog/demo");
        group.MapPost("/reset-and-seed", ResetAndSeedAsync);
        group.MapGet("/status", StatusAsync);
    }

    private static async Task<IResult> ResetAndSeedAsync(
        CatalogDemoResetAndSeedHost host,
        IHostEnvironment environment,
        IOptions<CatalogDemoSeedOptions> options,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        CancellationToken cancellationToken)
    {
        if (environment.IsProduction())
        {
            return Results.Problem(
                title: "Catalog demo reset/seed is blocked in Production.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "catalog.demo.production_blocked" });
        }

        if (!(environment.IsDevelopment() || environment.IsEnvironment("Testing")))
        {
            return Results.Problem(
                title: "Catalog demo reset/seed requires Development or Testing.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "catalog.demo.env_blocked" });
        }

        if (!options.Value.AllowResetAndSeed)
        {
            return Results.Problem(
                title: "Catalog demo reset/seed requires Tooba:CatalogDemo:AllowResetAndSeed=true.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "catalog.demo.opt_in_required" });
        }

        try
        {
            await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var result = await host.ExecuteAsync(printPlan: true, cancellationToken);
            return Results.Ok(new
            {
                reset = result.Reset,
                counts = result.Counts,
                plan = result.Plan,
            });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "catalog.demo.failed" });
        }
    }

    private static async Task<IResult> StatusAsync(
        CatalogDemoResetAndSeedHost host,
        IHostEnvironment environment,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        CancellationToken cancellationToken)
    {
        if (environment.IsProduction())
        {
            return Results.Problem(
                title: "Catalog demo status is blocked in Production.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "catalog.demo.production_blocked" });
        }

        await AdminPanelAccess.RequireAuthorizedAsync(
            request, session, tenant, guard, environment, cancellationToken);
        var status = await host.GetStatusAsync(cancellationToken);
        return Results.Ok(status);
    }
}
