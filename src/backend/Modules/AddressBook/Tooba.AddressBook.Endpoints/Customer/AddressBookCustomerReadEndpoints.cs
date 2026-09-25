using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.AddressBook.Application.Customer.Get;
using Tooba.AddressBook.Application.Customer.List;

namespace Tooba.AddressBook.Endpoints.Customer;

/// <summary>مرز HTTP خصوصی دفترچهٔ آدرس — فرمان/کوئری فقط از طریق ISender و بدون دسترسی مستقیم به دایرکتوری.</summary>
public static class AddressBookCustomerReadEndpoints
{
    /// <summary>دو مسیر خواندن (لیست و یک نشانی) را زیر مرز مشتری ثبت می‌کند.</summary>
    public static void MapReads(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("", ListAsync);
        group.MapGet("/{addressId:guid}", GetAsync);
    }

    private static async Task<IResult> ListAsync(
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

        var items = await sender.Send(new ListCustomerAddressesQuery(actor.Value), cancellationToken);
        return Results.Json(items);
    }

    private static async Task<IResult> GetAsync(
        Guid addressId,
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

        var item = await sender.Send(new GetCustomerAddressQuery(actor.Value, addressId), cancellationToken);
        return item is null
            ? Results.Json(new { title = "Not Found", errorCode = "customer.address.missing" }, statusCode: StatusCodes.Status404NotFound)
            : Results.Json(item);
    }

    private static IResult Unauthorized() => Results.Json(
        new { title = "Unauthorized", errorCode = "customer.session.required" },
        statusCode: StatusCodes.Status401Unauthorized);
}
