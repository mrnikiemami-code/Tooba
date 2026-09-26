using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.AddressBook.Application;
using Tooba.AddressBook.Application.Customer.Create;
using Tooba.AddressBook.Application.Customer.Update;

namespace Tooba.AddressBook.Endpoints.Customer;

/// <summary>
/// مرز HTTP نوشتن دفترچهٔ آدرس مشتری — ایجاد و ویرایش. فرمان‌ها فقط از طریق <see cref="ISender"/>
/// فرستاده می‌شوند و این لایه هیچ دسترسی مستقیمی به <c>IAddressBookDirectory</c> ندارد.
/// </summary>
public static class AddressBookCustomerWriteEndpoints
{
    /// <summary>دو مسیر نوشتن (ایجاد و ویرایش) را زیر مرز مشتری ثبت می‌کند.</summary>
    public static void MapWrites(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapPost("", CreateAsync);
        group.MapPut("/{addressId:guid}", UpdateAsync);
    }

    private static async Task<IResult> CreateAsync(
        CustomerAddressWriteRequest body,
        HttpContext httpContext,
        IAddressBookCustomerActorResolver actorResolver,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var actor = actorResolver.ResolveActor(httpContext);
        if (actor is null)
        {
            return Unauthorized();
        }

        var created = await sender.Send(
            new CreateCustomerAddressCommand(actor.Value, body.ToWrite()),
            cancellationToken);
        return Results.Json(created, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> UpdateAsync(
        Guid addressId,
        CustomerAddressWriteRequest body,
        HttpContext httpContext,
        IAddressBookCustomerActorResolver actorResolver,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var actor = actorResolver.ResolveActor(httpContext);
        if (actor is null)
        {
            return Unauthorized();
        }

        var updated = await sender.Send(
            new UpdateCustomerAddressCommand(actor.Value, addressId, body.ToWrite()),
            cancellationToken);
        return Results.Json(updated);
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
    bool IsDefault,
    string? FirstName = null,
    string? LastName = null);

/// <summary>تبدیل بدنهٔ HTTP به ورودی ماژول بدون انتقال هویت مالک.</summary>
public static class CustomerAddressWriteRequestExtensions
{
    /// <summary>ورودی HTTP را به ورودی دایرکتوری تبدیل می‌کند.</summary>
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
            body.IsDefault,
            body.FirstName ?? string.Empty,
            body.LastName ?? string.Empty);
}
