using Tooba.AddressBook.Application;

namespace Tooba.Host.AddressBook;

/// <summary>مرز HTTP باقی‌ماندهٔ دفترچهٔ آدرس (حذف/پیش‌فرض) که Actor را فقط از نشست یا seam کنترل‌شدهٔ توسعه می‌گیرد.</summary>
public static class AddressBookEndpoints
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <summary>مسیرهای باقی‌مانده (حذف/پیش‌فرض) را ثبت می‌کند؛ خواندن‌ها و ایجاد/ویرایش ماژول‌محور شده‌اند.</summary>
    public static void MapAddressBookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/customer/addresses");
        group.MapDelete("/{addressId:guid}", DeleteAsync);
        group.MapPost("/{addressId:guid}/default", SetDefaultAsync);
    }

    private static async Task<IResult> DeleteAsync(
        Guid addressId,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        IAddressBookDirectory addresses,
        CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        if (actor is null)
        {
            return Unauthorized();
        }

        await addresses.DeleteAsync(actor.Value, addressId, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> SetDefaultAsync(
        Guid addressId,
        HttpRequest request,
        CurrentAuthenticatedSession session,
        IHostEnvironment environment,
        IAddressBookDirectory addresses,
        CancellationToken cancellationToken)
    {
        var actor = ResolveActor(request, session, environment);
        if (actor is null)
        {
            return Unauthorized();
        }

        return Results.Json(await addresses.SetDefaultAsync(actor.Value, addressId, cancellationToken));
    }

    /// <summary>
    /// Actor را از نشست معتبر، سپس هدر توسعه، سپس شناسهٔ مهمان فروشگاه در Dev/Testing می‌گیرد.
    /// Production بدون نشست 401 است و بدنهٔ درخواست هویت نمی‌سازد.
    /// </summary>
    internal static Guid? ResolveActor(
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
            && Guid.TryParse(raw.ToString(), out var actor)
            && actor != Guid.Empty)
        {
            return actor;
        }

        return Tooba.Order.Contracts.Fulfillment.StorefrontGuestActor.ActorId;
    }

    private static IResult Unauthorized() => Results.Json(
        new { title = "Unauthorized", errorCode = "customer.session.required" },
        statusCode: StatusCodes.Status401Unauthorized);
}
