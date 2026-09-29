using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Party.Application.Seller.Commands;
using Tooba.Party.Application.Seller.Models;
using Tooba.Party.Application.Seller.Queries;

namespace Tooba.Party.Endpoints.Seller;

/// <summary>
/// مسیرهای تنظیمات فروشنده — مالکیت Party via MediatR و <see cref="ApiResponseFactory"/>.
/// رفتار قبلی Host حفظ می‌شود: <c>GET/PUT /v1/seller/settings</c>، شکل的成功 body با
/// <c>partyId</c> و <c>canManage</c>، و کدهای پایدار <c>seller.settings.*</c>.
/// </summary>
public static class PartySellerSettingsEndpoints
{
    /// <summary>مسیرهای تنظیمات فروشنده را زیر گروه <c>/v1/seller/settings</c> ثبت می‌کند.</summary>
    public static void Map(RouteGroupBuilder group)
    {
        ArgumentNullException.ThrowIfNull(group);
        group.MapGet("/", GetSettingsAsync);
        group.MapPut("/", UpdateSettingsAsync);
    }

    private static async Task<IResult> GetSettingsAsync(
        ISender sender,
        IPartySellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var (_, sellerPartyId, canManage) = await authorizer.RequireViewAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new GetSellerSettingsQuery(sellerPartyId, canManage),
            cancellationToken));
    }

    private static async Task<IResult> UpdateSettingsAsync(
        PartySellerSettingsWriteRequest body,
        ISender sender,
        IPartySellerAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext http,
        CancellationToken cancellationToken)
    {
        var (_, sellerPartyId) = await authorizer.RequireManageAsync(http, cancellationToken);
        return api.From(await sender.Send(
            new UpdateSellerSettingsCommand(
                sellerPartyId,
                new PartySellerSettingsWriteModel(
                    body.DisplayName,
                    body.LegalName,
                    body.Description,
                    body.SupportPhone,
                    body.SupportEmail,
                    body.AddressLine)),
            cancellationToken));
    }
}

/// <summary>
/// بدنهٔ ویرایش تنظیمات سازمانی فروشنده — انتقال HTTP پنل فروشنده، نه قرارداد مشترک.
/// شکل آن برای سازگاری کلاینت منتشرشده بدون تغییر از Host منتقل شده است.
/// </summary>
public sealed record PartySellerSettingsWriteRequest(
    string DisplayName,
    string? LegalName,
    string? Description,
    string? SupportPhone,
    string? SupportEmail,
    string? AddressLine);
