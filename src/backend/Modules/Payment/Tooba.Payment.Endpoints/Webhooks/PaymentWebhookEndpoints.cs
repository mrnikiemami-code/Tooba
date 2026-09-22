using System.Text;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Payment.Application.Commands.ProcessPaymentWebhook;
using Tooba.Payment.Application.Ports;

namespace Tooba.Payment.Endpoints.Webhooks;

/// <summary>Thin Payment webhook HTTP route.</summary>
public static class PaymentWebhookEndpoints
{
    /// <summary>Maps POST /v1/payments/webhooks/{providerCode}.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.MapPost("/v1/payments/webhooks/{providerCode}", HandleAsync);
    }

    private static async Task<IResult> HandleAsync(
        string providerCode,
        HttpRequest request,
        ISender sender,
        IPaymentWebhookSignatureVerifier signatures,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var bodyText = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        request.Body.Position = 0;
        var bodyBytes = Encoding.UTF8.GetBytes(bodyText);

        var result = await sender.Send(
            new ProcessPaymentWebhookCommand(
                providerCode,
                bodyBytes,
                request.Headers[signatures.SignatureHeaderName],
                bodyText),
            cancellationToken).ConfigureAwait(false);

        if (!result.IsSuccess)
            return api.From(result);

        return Results.Json(new
        {
            accepted = true,
            duplicate = result.Value!.Duplicate,
        });
    }
}
