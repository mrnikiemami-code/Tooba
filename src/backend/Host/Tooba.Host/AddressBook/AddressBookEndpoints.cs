using Tooba.AddressBook.Application;
using Tooba.Host.Storefront;

namespace Tooba.Host.AddressBook;

/// <summary>مرز HTTP خصوصی دفترچهٔ آدرس که Actor را فقط از نشست یا seam کنترل‌شدهٔ توسعه می‌گیرد.</summary>
public static class AddressBookEndpoints
{
    private const string DevActorHeader = "X-Tooba-Dev-Actor-User-Id";

    /// <summary>مسیرهای CRUD و پیش‌فرض را زیر مرز مشتری ثبت می‌کند.</summary>
    public static void MapAddressBookEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/v1/customer/addresses");
        group.MapGet("", ListAsync);
        group.MapGet("/{addressId:guid}", GetAsync);
        group.MapPost("", CreateAsync);
        group.MapPut("/{addressId:guid}", UpdateAsync);
        group.MapDelete("/{addressId:guid}", DeleteAsync);
        group.MapPost("/{addressId:guid}/default", SetDefaultAsync);
    }

    private static async Task<IResult> ListAsync(
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

        return Results.Json(await addresses.ListAsync(actor.Value, cancellationToken));
    }

    private static async Task<IResult> GetAsync(
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

        var item = await addresses.GetAsync(actor.Value, addressId, cancellationToken);
        return item is null
            ? Results.Json(new { title = "Not Found", errorCode = "customer.address.missing" }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(item);
    }

    private static async Task<IResult> CreateAsync(
        CustomerAddressWriteRequest body,
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

        var created = await addresses.CreateAsync(actor.Value, body.ToWrite(), cancellationToken);
        return Results.Json(created, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> UpdateAsync(
        Guid addressId,
        CustomerAddressWriteRequest body,
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

        var updated = await addresses.UpdateAsync(actor.Value, addressId, body.ToWrite(), cancellationToken);
        return Results.Json(updated);
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

        return StorefrontCheckoutComposer.StorefrontGuestActorId;
    }

    private static IResult Unauthorized() => Results.Json(
        new { title = "Unauthorized", errorCode = "customer.session.required" },
        statusCode: StatusCodes.Status401Unauthorized);
}

/// <summary>بدنهٔ ایجاد/ویرایش نشانی؛ شناسهٔ مالک را از کلاینت نمی‌پذیرد.</summary>
public sealed record CustomerAddressWriteRequest(
    string RecipientName,
    string ContactMobile,
    string? Country,
    string? ProvinceName,
    string CityName,
    string PostalCode,
    string PostalAddress,
    string? BuildingUnit,
    string? Label,
    bool IsDefault);

/// <summary>تبدیل بدنهٔ HTTP به فرمان ماژول بدون انتقال هویت مالک.</summary>
public static class CustomerAddressWriteRequestExtensions
{
    /// <summary>ورودی HTTP را به فرمان دایرکتوری تبدیل می‌کند.</summary>
    public static CustomerAddressWrite ToWrite(this CustomerAddressWriteRequest body) =>
        new(
            body.RecipientName,
            body.ContactMobile,
            body.Country,
            body.ProvinceName,
            body.CityName,
            body.PostalCode,
            body.PostalAddress,
            body.BuildingUnit,
            body.Label,
            body.IsDefault);
}
