using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.CustomerProfile.Contracts;
using Tooba.Host.Storefront;
using Tooba.Order.Application.Customer.Models;
using Tooba.Order.Application.Customer.Queries.GetCustomerOrderDashboardSummary;
using Tooba.Order.Contracts.Fulfillment;

namespace Tooba.Host.Customer;

/// <summary>
/// مرز HTTP پنل مشتری. اصل تولید فقط از نشست معتبر می‌آید؛
/// هدر Actor موجود صرفاً seam محیط Development برای شواهد محلی است.
/// مسیرهای /orders* به Order.Endpoints منتقل شده‌اند.
/// </summary>
public static class CustomerPanelEndpoints
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <summary>
    /// مسیرهای خواندنی پنل مشتری را ثبت می‌کند (بدون /orders*).
    /// </summary>
    public static void MapCustomerPanelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/customer");
        group.MapGet("/dev-context", GetDevContext);
        group.MapGet("/dashboard", GetDashboardAsync);
        group.MapGet("/profile", GetProfileAsync);
        group.MapPut("/profile", UpdateProfileAsync);
    }

    private static IResult GetDevContext(IHostEnvironment environment)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return Results.NotFound();
        }

        return Results.Json(new
        {
            actorUserId = StorefrontGuestActor.ActorId,
            label = "مشتری آزمایشی فروشگاه",
        });
    }

    private static async Task<IResult> GetDashboardAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CustomerPanelComposer composer,
        MediatR.ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        if (actor is null)
        {
            return Unauthorized(api);
        }

        var summaryResult = await sender.Send(
            new GetCustomerOrderDashboardSummaryQuery(actor.Value),
            cancellationToken);
        if (summaryResult.IsFailure)
        {
            return api.FromFailure(summaryResult.FirstError);
        }

        return Results.Json(
            await composer.ComposeDashboardAsync(actor.Value, summaryResult.Value, cancellationToken));
    }

    private static async Task<IResult> GetProfileAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CustomerPanelComposer composer,
        MediatR.ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        if (actor is null)
        {
            return Unauthorized(api);
        }

        var summaryResult = await sender.Send(
            new GetCustomerOrderDashboardSummaryQuery(actor.Value),
            cancellationToken);
        CustomerOrderDashboardSummary? summary = summaryResult.IsSuccess ? summaryResult.Value : null;
        return Results.Json(
            await composer.ComposeProfileAsync(actor.Value, summary, cancellationToken));
    }

    private static async Task<IResult> UpdateProfileAsync(
        CustomerProfileWriteRequest body,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CustomerPanelComposer composer,
        MediatR.ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        if (actor is null)
        {
            return Unauthorized(api);
        }

        await composer.UpsertProfileAsync(actor.Value, body.ToWrite(), cancellationToken);
        var summaryResult = await sender.Send(
            new GetCustomerOrderDashboardSummaryQuery(actor.Value),
            cancellationToken);
        CustomerOrderDashboardSummary? summary = summaryResult.IsSuccess ? summaryResult.Value : null;
        return Results.Json(
            await composer.ComposeProfileAsync(actor.Value, summary, cancellationToken));
    }

    private static Guid? ResolveActor(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment)
    {
        if (session.IsAuthenticated)
        {
            return session.UserId;
        }

        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return null;
        }

        if (request.Headers.TryGetValue(DevActorHeader, out var raw)
            && Guid.TryParse(raw.ToString(), out var devActor)
            && devActor != Guid.Empty)
        {
            return devActor;
        }

        return StorefrontGuestActor.ActorId;
    }

    private static IResult Unauthorized(ApiResponseFactory api) =>
        api.FromFailure(new SemanticError("customer.session.required"));
}

/// <summary>بدنهٔ ویرایش پروفایل؛ شناسه‌های Identity و credential دریافت نمی‌کند.</summary>
public sealed record CustomerProfileWriteRequest(
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? BirthDate,
    string? Bio);

/// <summary>تبدیل بدنهٔ HTTP به فرمان ماژول بدون انتقال هویت مالک.</summary>
public static class CustomerProfileWriteRequestExtensions
{
    /// <summary>ورودی HTTP را به فرمان دایرکتوری تبدیل می‌کند.</summary>
    public static CustomerProfileWrite ToWrite(this CustomerProfileWriteRequest body) =>
        new(body.DisplayName, body.FirstName, body.LastName, body.BirthDate, body.Bio);
}
