using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BulkInquiry.Application.Models;
using Tooba.BulkInquiry.Application.Storefront.Commands;

namespace Tooba.BulkInquiry.Endpoints.Storefront;

/// <summary>مرز HTTP عمومی BulkInquiry.</summary>
public static class BulkInquiryStorefrontEndpoints
{
    /// <summary>مسیر ثبت درخواست عمده را ثبت می‌کند.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapPost("/v1/storefront/products/{slug}/bulk-inquiries", SubmitAsync);
    }

    private static async Task<IResult> SubmitAsync(
        string slug,
        BulkInquiryBody body,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        var request = new SubmitBulkInquiryRequest(
            slug, body.FullName, body.Phone, body.Email, body.CompanyName,
            body.Address, body.Quantity, body.Notes);
        var result = await sender.Send(new SubmitBulkInquiryCommand(request), cancellationToken);
        var location = result.IsSuccess
            ? $"/v1/storefront/products/{slug}/bulk-inquiries/{result.Value.InquiryId}"
            : $"/v1/storefront/products/{slug}/bulk-inquiries";
        return api.Created(location, result);
    }
}

/// <summary>بدنهٔ HTTP درخواست خرید عمده؛ slug از مسیر تأمین می‌شود.</summary>
public sealed record BulkInquiryBody(
    string FullName,
    string Phone,
    string? Email,
    string? CompanyName,
    string Address,
    decimal Quantity,
    string? Notes);
