using Tooba.CustomerProfile.Application;
using Tooba.Host.Storefront;

namespace Tooba.Host.Customer;

/// <summary>
/// مرز HTTP پنل مشتری. اصل تولید فقط از نشست معتبر می‌آید؛
/// هدر Actor موجود صرفاً seam محیط Development برای شواهد محلی است.
/// </summary>
public static class CustomerPanelEndpoints
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <summary>
    /// مسیرهای خواندنی پنل مشتری را ثبت می‌کند.
    /// </summary>
    public static void MapCustomerPanelEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/customer");
        group.MapGet("/dev-context", GetDevContext);
        group.MapGet("/dashboard", GetDashboardAsync);
        group.MapGet("/profile", GetProfileAsync);
        group.MapPut("/profile", UpdateProfileAsync);
        group.MapGet("/orders", ListOrdersAsync);
        group.MapGet("/orders/{checkoutId:guid}", GetOrderAsync);
    }

    private static IResult GetDevContext(IHostEnvironment environment)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
        {
            return Results.NotFound();
        }

        return Results.Json(new
        {
            actorUserId = StorefrontCheckoutComposer.StorefrontGuestActorId,
            label = "مشتری آزمایشی فروشگاه",
        });
    }

    private static async Task<IResult> GetDashboardAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CustomerPanelComposer composer,
        CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        return actor is null
            ? Unauthorized()
            : Results.Json(await composer.GetDashboardAsync(actor.Value, cancellationToken));
    }

    private static async Task<IResult> GetProfileAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CustomerPanelComposer composer,
        CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        return actor is null
            ? Unauthorized()
            : Results.Json(await composer.GetProfileAsync(actor.Value, cancellationToken));
    }

    private static async Task<IResult> UpdateProfileAsync(
        CustomerProfileWriteRequest body,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CustomerPanelComposer composer,
        CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        if (actor is null)
        {
            return Unauthorized();
        }

        var updated = await composer.UpdateProfileAsync(actor.Value, body.ToWrite(), cancellationToken);
        return Results.Json(updated);
    }

    private static async Task<IResult> ListOrdersAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CustomerPanelComposer composer,
        CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        return actor is null
            ? Unauthorized()
            : Results.Json(await composer.ListOrdersAsync(actor.Value, cancellationToken));
    }

    private static async Task<IResult> GetOrderAsync(
        Guid checkoutId,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        CustomerPanelComposer composer,
        CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        if (actor is null)
        {
            return Unauthorized();
        }

        var page = await composer.GetOrderAsync(actor.Value, checkoutId, cancellationToken);
        return page is null
            ? Results.Json(
                new { title = "Not Found", errorCode = "customer.order.missing" },
                statusCode: StatusCodes.Status404NotFound)
            : Results.Json(page);
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

        return StorefrontCheckoutComposer.StorefrontGuestActorId;
    }

    private static IResult Unauthorized() =>
        Results.Json(
            new { title = "Unauthorized", errorCode = "customer.session.required" },
            statusCode: StatusCodes.Status401Unauthorized);
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
