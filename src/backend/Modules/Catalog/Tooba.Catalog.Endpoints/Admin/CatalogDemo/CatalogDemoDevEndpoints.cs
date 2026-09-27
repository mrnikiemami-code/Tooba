using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Tooba.BuildingBlocks;
using Tooba.Catalog.Application.Development.CatalogDemo;

namespace Tooba.Catalog.Endpoints.Admin.CatalogDemo;

/// <summary>
/// مسیرهای Development/Testing برای reset+seed و وضعیت Catalog Demo.
/// Route ownership: Catalog.Endpoints — preserves <c>/v1/admin/catalog/demo/...</c>.
/// </summary>
public static class CatalogDemoDevEndpoints
{
    /// <summary>ثبت مسیرهای demo catalog.</summary>
    public static IEndpointRouteBuilder MapCatalogDemoDevEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/admin/catalog/demo");
        group.MapPost("/reset-and-seed", ResetAndSeedAsync);
        group.MapPost("/seed-only", SeedOnlyAsync);
        group.MapGet("/status", StatusAsync);
        group.MapGet("/assignment-integrity", AssignmentIntegrityAsync);
        group.MapPost("/assignment-integrity/cleanup", AssignmentIntegrityCleanupAsync);
        return app;
    }

    private static async Task<IResult> ResetAndSeedAsync(
        ICatalogDemoResetAndSeedGateway host,
        IHostEnvironment environment,
        IOptions<CatalogDemoSeedOptions> options,
        HttpContext http,
        ICatalogAdminAuthorizer auth,
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
            await auth.RequireAuthorizedAsync(http, cancellationToken);
            var result = await host.ExecuteAsync(printPlan: true, cancellationToken);
            return Results.Ok(new
            {
                reset = result.Reset,
                counts = result.Counts,
                plan = result.Plan,
                assignmentIntegrity = result.AssignmentIntegrity,
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

    private static async Task<IResult> SeedOnlyAsync(
        ICatalogDemoResetAndSeedGateway host,
        IHostEnvironment environment,
        IOptions<CatalogDemoSeedOptions> options,
        HttpContext http,
        ICatalogAdminAuthorizer auth,
        CancellationToken cancellationToken)
    {
        if (environment.IsProduction())
        {
            return Results.Problem(
                title: "Catalog demo seed is blocked in Production.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "catalog.demo.production_blocked" });
        }

        if (!(environment.IsDevelopment() || environment.IsEnvironment("Testing")))
        {
            return Results.Problem(
                title: "Catalog demo seed requires Development or Testing.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "catalog.demo.env_blocked" });
        }

        if (!options.Value.AllowResetAndSeed)
        {
            return Results.Problem(
                title: "Catalog demo seed requires Tooba:CatalogDemo:AllowResetAndSeed=true.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "catalog.demo.opt_in_required" });
        }

        try
        {
            await auth.RequireAuthorizedAsync(http, cancellationToken);
            var counts = await host.SeedOnlyAsync(cancellationToken);
            return Results.Ok(new { counts });
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
        ICatalogDemoResetAndSeedGateway host,
        IHostEnvironment environment,
        HttpContext http,
        ICatalogAdminAuthorizer auth,
        CancellationToken cancellationToken)
    {
        if (environment.IsProduction())
        {
            return Results.Problem(
                title: "Catalog demo status is blocked in Production.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "catalog.demo.production_blocked" });
        }

        await auth.RequireAuthorizedAsync(http, cancellationToken);
        var status = await host.GetStatusAsync(cancellationToken);
        return Results.Ok(status);
    }

    private static async Task<IResult> AssignmentIntegrityAsync(
        ICatalogDemoResetAndSeedGateway host,
        IHostEnvironment environment,
        HttpContext http,
        ICatalogAdminAuthorizer auth,
        CancellationToken cancellationToken)
    {
        if (environment.IsProduction())
        {
            return Results.Problem(
                title: "Catalog demo assignment integrity is blocked in Production.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "catalog.demo.production_blocked" });
        }

        await auth.RequireAuthorizedAsync(http, cancellationToken);
        var audit = await host.AuditAssignmentsAsync(cancellationToken);
        return Results.Ok(audit);
    }

    private static async Task<IResult> AssignmentIntegrityCleanupAsync(
        ICatalogDemoResetAndSeedGateway host,
        IHostEnvironment environment,
        IOptions<CatalogDemoSeedOptions> options,
        HttpContext http,
        ICatalogAdminAuthorizer auth,
        CancellationToken cancellationToken)
    {
        if (environment.IsProduction())
        {
            return Results.Problem(
                title: "Catalog demo assignment cleanup is blocked in Production.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "catalog.demo.production_blocked" });
        }

        if (!(environment.IsDevelopment() || environment.IsEnvironment("Testing")))
        {
            return Results.Problem(
                title: "Catalog demo assignment cleanup requires Development or Testing.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "catalog.demo.env_blocked" });
        }

        if (!options.Value.AllowResetAndSeed)
        {
            return Results.Problem(
                title: "Catalog demo assignment cleanup requires Tooba:CatalogDemo:AllowResetAndSeed=true.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "catalog.demo.opt_in_required" });
        }

        try
        {
            await auth.RequireAuthorizedAsync(http, cancellationToken);
            var result = await host.CleanupAssignmentsAsync(cancellationToken);
            return Results.Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(
                title: ex.Message,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: new Dictionary<string, object?> { ["errorCode"] = "catalog.demo.failed" });
        }
    }
}
