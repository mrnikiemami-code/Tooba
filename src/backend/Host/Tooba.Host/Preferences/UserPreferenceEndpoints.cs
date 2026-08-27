using Tooba.BuildingBlocks;
using Tooba.Host.Admin;
using Tooba.Host.Storefront;
using Tooba.UserPreference.Application;
using UserPreferenceEntity = Tooba.UserPreference.Domain.UserPreference;

namespace Tooba.Host.Preferences;

/// <summary>
/// مرز HTTP ترجیح locale برای مشتری و اپراتور؛ مالکیت فقط از Actor حل‌شده می‌آید.
/// </summary>
public static class UserPreferenceEndpoints
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <summary>مسیرهای ترجیح مشتری و اپراتور را ثبت می‌کند.</summary>
    public static void MapUserPreferenceEndpoints(this WebApplication app)
    {
        var customer = app.MapGroup("/v1/customer/preferences");
        customer.MapGet("/", GetCustomerPreferenceAsync);
        customer.MapPut("/", UpdateCustomerPreferenceAsync);

        var admin = app.MapGroup("/v1/admin/operator/preferences");
        admin.MapGet("/", GetAdminPreferenceAsync);
        admin.MapPut("/", UpdateAdminPreferenceAsync);
    }

    private static async Task<IResult> GetCustomerPreferenceAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        IUserPreferenceDirectory directory,
        CancellationToken cancellationToken)
    {
        var actor = ResolveCustomerActor(request, session, environment);
        if (actor is null)
        {
            return UnauthorizedCustomer();
        }

        var snapshot = await directory.GetAsync(actor.Value, cancellationToken);
        return Results.Json(snapshot is null
            ? new { locale = UserPreferenceEntity.LocaleFa, createdAt = (DateTimeOffset?)null, updatedAt = (DateTimeOffset?)null }
            : new { locale = snapshot.Locale, createdAt = snapshot.CreatedAt, updatedAt = snapshot.UpdatedAt });
    }

    private static async Task<IResult> UpdateCustomerPreferenceAsync(
        UserPreferenceWriteRequest body,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        IUserPreferenceDirectory directory,
        CancellationToken cancellationToken)
    {
        var actor = ResolveCustomerActor(request, session, environment);
        if (actor is null)
        {
            return UnauthorizedCustomer();
        }

        try
        {
            var updated = await directory.UpsertAsync(actor.Value, new UserPreferenceWrite(body.Locale), cancellationToken);
            return Results.Json(new { locale = updated.Locale, createdAt = updated.CreatedAt, updatedAt = updated.UpdatedAt });
        }
        catch (InvalidOperationException)
        {
            return Results.Json(
                new { title = "Rejected", errorCode = "preference.rejected" },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static async Task<IResult> GetAdminPreferenceAsync(
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IUserPreferenceDirectory directory,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var snapshot = await directory.GetAsync(actor, cancellationToken);
            return Results.Json(snapshot is null
                ? new { locale = UserPreferenceEntity.LocaleFa, createdAt = (DateTimeOffset?)null, updatedAt = (DateTimeOffset?)null }
                : new { locale = snapshot.Locale, createdAt = snapshot.CreatedAt, updatedAt = snapshot.UpdatedAt });
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
    }

    private static async Task<IResult> UpdateAdminPreferenceAsync(
        UserPreferenceWriteRequest body,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        ICurrentTenant tenant,
        IAuthorizationGuard guard,
        IHostEnvironment environment,
        IUserPreferenceDirectory directory,
        CancellationToken cancellationToken)
    {
        try
        {
            var actor = await AdminPanelAccess.RequireAuthorizedAsync(
                request, session, tenant, guard, environment, cancellationToken);
            var updated = await directory.UpsertAsync(actor, new UserPreferenceWrite(body.Locale), cancellationToken);
            return Results.Json(new { locale = updated.Locale, createdAt = updated.CreatedAt, updatedAt = updated.UpdatedAt });
        }
        catch (PlatformHttpException ex)
        {
            return Results.Json(new { title = ex.Message, errorCode = ex.ErrorCode }, statusCode: ex.StatusCode);
        }
        catch (InvalidOperationException)
        {
            return Results.Json(
                new { title = "Rejected", errorCode = "preference.rejected" },
                statusCode: StatusCodes.Status400BadRequest);
        }
    }

    private static Guid? ResolveCustomerActor(
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

    private static IResult UnauthorizedCustomer() =>
        Results.Json(
            new { title = "Unauthorized", errorCode = "customer.session.required" },
            statusCode: StatusCodes.Status401Unauthorized);
}

/// <summary>بدنهٔ نوشتن locale.</summary>
public sealed record UserPreferenceWriteRequest(string Locale);
