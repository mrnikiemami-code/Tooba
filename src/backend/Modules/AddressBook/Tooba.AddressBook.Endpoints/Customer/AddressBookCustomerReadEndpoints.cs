using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.AddressBook.Application.Addresses.Queries;
using Tooba.AddressBook.Contracts.Errors;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;

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
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = actorResolver.ResolveActor(httpContext);
        if (actor is null)
        {
            return api.FromFailure(new SemanticError(AddressBookErrorCodes.SessionRequired));
        }

        var result = await sender.Send(new ListCustomerAddressesQuery(actor.Value), cancellationToken);
        return api.From(result);
    }

    private static async Task<IResult> GetAsync(
        Guid addressId,
        HttpContext httpContext,
        IAddressBookCustomerActorResolver actorResolver,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var actor = actorResolver.ResolveActor(httpContext);
        if (actor is null)
        {
            return api.FromFailure(new SemanticError(AddressBookErrorCodes.SessionRequired));
        }

        var result = await sender.Send(new GetCustomerAddressQuery(actor.Value, addressId), cancellationToken);
        return api.From(result);
    }
}
