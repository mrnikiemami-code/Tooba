using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Commands.CompleteSandboxPayment;
using Tooba.Payment.Application.Commands.InitiateStorefrontPayment;
using Tooba.Payment.Application.Commands.RetryManualPayment;
using Tooba.Payment.Application.Commands.RetryUnpaidPayment;
using Tooba.Payment.Application.Commands.SubmitManualPaymentEvidence;
using Tooba.Payment.Application.Commands.UploadManualPaymentProof;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Queries.GetStorefrontPayment;
using Tooba.Payment.Application.Queries.GetStorefrontPaymentSandboxContext;
using Tooba.Payment.Application.Queries.GetStorefrontWalletQuote;
using Tooba.Payment.Application.Queries.ListStorefrontPaymentMethods;

namespace Tooba.Payment.Endpoints.Storefront;

/// <summary>Thin storefront Payment HTTP routes.</summary>
public static class PaymentStorefrontEndpoints
{
    /// <summary>Maps storefront Payment routes under /v1/storefront.</summary>
    public static void Map(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var group = app.MapGroup("/v1/storefront");
        group.MapPost("/checkout/{checkoutId:guid}/payments", InitiateAsync);
        group.MapGet("/checkout/{checkoutId:guid}/wallet-quote", WalletQuoteAsync);
        group.MapGet("/payment-methods", MethodsAsync);
        group.MapGet("/payments/{paymentId:guid}", GetAsync);
        group.MapGet("/payments/{paymentId:guid}/sandbox", SandboxContextAsync);
        group.MapPost("/payments/{paymentId:guid}/sandbox/complete", SandboxCompleteAsync);
        group.MapPost("/payments/{paymentId:guid}/manual-evidence", ManualEvidenceAsync);
        group.MapPost("/payments/{paymentId:guid}/manual-retry", ManualRetryAsync);
        group.MapPost("/payments/{paymentId:guid}/unpaid-retry", UnpaidRetryAsync);
        group.MapPost("/payments/{paymentId:guid}/proof", UploadProofAsync).DisableAntiforgery();
    }

    private static async Task<IResult> InitiateAsync(
        Guid checkoutId,
        InitiatePaymentBody body,
        ISender sender,
        IPaymentStorefrontAuthorizer authorizer,
        ApiResponseFactory api,
        IIdGenerator ids,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var idem = ResolveIdempotencyKey(body.IdempotencyKey, context.Request, ids);
        return api.From(await sender.Send(
            new InitiateStorefrontPaymentCommand(
                checkoutId,
                body.CartId,
                ReadGuestSecret(context.Request),
                idem,
                body.WantsWallet,
                body.ProviderCode,
                authorizer.TryResolveAuthenticatedUserId(context)),
            cancellationToken));
    }

    private static async Task<IResult> WalletQuoteAsync(
        Guid checkoutId,
        Guid cartId,
        ISender sender,
        IPaymentStorefrontAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(
            new GetStorefrontWalletQuoteQuery(
                checkoutId,
                cartId,
                ReadGuestSecret(context.Request),
                authorizer.TryResolveAuthenticatedUserId(context)),
            cancellationToken));

    private static async Task<IResult> MethodsAsync(
        ISender sender, ApiResponseFactory api, CancellationToken cancellationToken) =>
        api.From(await sender.Send(new ListStorefrontPaymentMethodsQuery(), cancellationToken));

    private static async Task<IResult> GetAsync(
        Guid paymentId,
        Guid cartId,
        ISender sender,
        IPaymentStorefrontAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(
            new GetStorefrontPaymentQuery(
                paymentId,
                cartId,
                ReadGuestSecret(context.Request),
                authorizer.TryResolveAuthenticatedUserId(context)),
            cancellationToken));

    private static async Task<IResult> SandboxContextAsync(
        Guid paymentId,
        Guid cartId,
        ISender sender,
        IPaymentStorefrontAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(
            new GetStorefrontPaymentSandboxContextQuery(
                paymentId,
                cartId,
                ReadGuestSecret(context.Request),
                authorizer.TryResolveAuthenticatedUserId(context)),
            cancellationToken));

    private static async Task<IResult> SandboxCompleteAsync(
        Guid paymentId,
        SandboxCompleteBody body,
        ISender sender,
        IPaymentStorefrontAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(
            new CompleteSandboxPaymentCommand(
                paymentId,
                body.CartId,
                ReadGuestSecret(context.Request),
                body.AttemptId,
                body.ProviderRequestReference,
                body.Outcome,
                authorizer.TryResolveAuthenticatedUserId(context)),
            cancellationToken));

    private static async Task<IResult> ManualEvidenceAsync(
        Guid paymentId,
        ManualEvidenceBody body,
        ISender sender,
        IPaymentStorefrontAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(
            new SubmitManualPaymentEvidenceCommand(
                paymentId,
                body.CartId,
                ReadGuestSecret(context.Request),
                body.TransferReference,
                body.ProofMediaAssetId,
                authorizer.TryResolveAuthenticatedUserId(context)),
            cancellationToken));

    private static async Task<IResult> ManualRetryAsync(
        Guid paymentId,
        PaymentCartBody body,
        ISender sender,
        IPaymentStorefrontAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(
            new RetryManualPaymentCommand(
                paymentId,
                body.CartId,
                ReadGuestSecret(context.Request),
                authorizer.TryResolveAuthenticatedUserId(context)),
            cancellationToken));

    private static async Task<IResult> UnpaidRetryAsync(
        Guid paymentId,
        PaymentCartBody body,
        ISender sender,
        IPaymentStorefrontAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken) =>
        api.From(await sender.Send(
            new RetryUnpaidPaymentCommand(
                paymentId,
                body.CartId,
                ReadGuestSecret(context.Request),
                authorizer.TryResolveAuthenticatedUserId(context)),
            cancellationToken));

    private static async Task<IResult> UploadProofAsync(
        Guid paymentId,
        Guid cartId,
        ISender sender,
        IPaymentStorefrontAuthorizer authorizer,
        ApiResponseFactory api,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!context.Request.HasFormContentType)
            return api.FromFailure(new SemanticError(PaymentErrorCodes.ProofRequired));

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null)
            return api.FromFailure(new SemanticError(PaymentErrorCodes.ProofRequired));

        await using var stream = file.OpenReadStream();
        var result = await sender.Send(
            new UploadManualPaymentProofCommand(
                paymentId,
                cartId,
                ReadGuestSecret(context.Request),
                stream,
                file.FileName,
                file.ContentType ?? string.Empty,
                authorizer.TryResolveAuthenticatedUserId(context)),
            cancellationToken);
        if (!result.IsSuccess)
            return api.From(result);
        return Results.Json(new { mediaAssetId = result.Value });
    }

    private static string? ReadGuestSecret(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Tooba-Guest-Secret", out var header) && !string.IsNullOrWhiteSpace(header))
            return header.ToString();
        return null;
    }

    private static string ResolveIdempotencyKey(string? bodyKey, HttpRequest request, IIdGenerator ids)
    {
        if (!string.IsNullOrWhiteSpace(bodyKey))
            return bodyKey.Trim();
        if (request.Headers.TryGetValue("Idempotency-Key", out var raw) && !string.IsNullOrWhiteSpace(raw))
            return raw.ToString().Trim();
        return ids.NewId().ToString("N");
    }
}
