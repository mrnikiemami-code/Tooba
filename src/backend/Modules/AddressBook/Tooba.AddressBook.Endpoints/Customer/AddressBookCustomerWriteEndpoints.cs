using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.AddressBook.Application.Addresses.Commands;
using Tooba.AddressBook.Application.Models;
using Tooba.AddressBook.Contracts.Errors;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;

namespace Tooba.AddressBook.Endpoints.Customer;

/// <summary>
/// مرز HTTP نوشتن دفترچهٔ آدرس مشتری — ایجاد، ویرایش، حذف و پیش‌فرض. فرمان‌ها فقط از طریق
/// <see cref="ISender"/> فرستاده می‌شوند و این لایه هیچ دسترسی مستقیمی به <c>IAddressBookDirectory</c> ندارد.
/// </summary>
public static class AddressBookCustomerWriteEndpoints
{
    /// <summary>مسیرهای نوشتن (ایجاد/ویرایش/حذف/پیش‌فرض) را زیر مرز مشتری ثبت می‌کند.</summary>
    public static void MapWrites(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapPost("", CreateAsync);
        group.MapPut("/{addressId:guid}", UpdateAsync);
        group.MapDelete("/{addressId:guid}", DeleteAsync);
        group.MapPost("/{addressId:guid}/default", SetDefaultAsync);
    }

    private static async Task<IResult> CreateAsync(
        CustomerAddressWriteRequest body,
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

        var created = await sender.Send(
            new CreateCustomerAddressCommand(actor.Value, body.ToWrite()),
            cancellationToken);
        return created.IsSuccess
            ? api.Created($"/v1/customer/addresses/{created.Value.AddressId}", created)
            : api.From(created);
    }

    private static async Task<IResult> UpdateAsync(
        Guid addressId,
        CustomerAddressWriteRequest body,
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

        var updated = await sender.Send(
            new UpdateCustomerAddressCommand(actor.Value, addressId, body.ToWrite()),
            cancellationToken);
        return api.From(updated);
    }

    private static async Task<IResult> DeleteAsync(
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

        var result = await sender.Send(
            new DeleteCustomerAddressCommand(actor.Value, addressId),
            cancellationToken);
        return api.From(result);
    }

    private static async Task<IResult> SetDefaultAsync(
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

        var updated = await sender.Send(
            new SetDefaultCustomerAddressCommand(actor.Value, addressId),
            cancellationToken);
        return api.From(updated);
    }
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
