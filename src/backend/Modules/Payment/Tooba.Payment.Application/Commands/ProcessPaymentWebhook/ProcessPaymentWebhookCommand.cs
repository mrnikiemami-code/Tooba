using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Payment.Application.Errors;
using Tooba.Payment.Application.Ports;

namespace Tooba.Payment.Application.Commands.ProcessPaymentWebhook;

/// <summary>Accepted webhook processing outcome.</summary>
public sealed record ProcessPaymentWebhookResult(bool Accepted, bool Duplicate);

/// <summary>MediatR process payment webhook use case.</summary>
public sealed record ProcessPaymentWebhookCommand(
    string ProviderCode,
    byte[] RawBody,
    string? SignatureHeader,
    string BodyText) : IRequest<Result<ProcessPaymentWebhookResult>>;

/// <summary>Validates signature/payload then delegates to webhook handler port.</summary>
public sealed class ProcessPaymentWebhookHandler(
    IPaymentWebhookSignatureVerifier signatures,
    IPaymentWebhookHandler handler)
    : IRequestHandler<ProcessPaymentWebhookCommand, Result<ProcessPaymentWebhookResult>>
{
    public async Task<Result<ProcessPaymentWebhookResult>> Handle(
        ProcessPaymentWebhookCommand request, CancellationToken cancellationToken)
    {
        if (!signatures.TryValidate(request.RawBody, request.SignatureHeader, out var signatureError))
            return Result.Failure<ProcessPaymentWebhookResult>(new SemanticError(
                string.IsNullOrWhiteSpace(signatureError)
                    ? PaymentErrorCodes.WebhookInvalidSignature
                    : signatureError));

        PaymentWebhookNotification? notification;
        try
        {
            notification = System.Text.Json.JsonSerializer.Deserialize<PaymentWebhookNotification>(
                request.BodyText,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (System.Text.Json.JsonException)
        {
            return Result.Failure<ProcessPaymentWebhookResult>(
                new SemanticError(PaymentErrorCodes.WebhookInvalidPayload));
        }

        if (notification is null
            || notification.PaymentId == Guid.Empty
            || notification.AttemptId == Guid.Empty
            || string.IsNullOrWhiteSpace(notification.ProviderEventId)
            || string.IsNullOrWhiteSpace(notification.ProviderRequestReference))
        {
            return Result.Failure<ProcessPaymentWebhookResult>(
                new SemanticError(PaymentErrorCodes.WebhookInvalidPayload));
        }

        var result = await handler.HandleAsync(request.ProviderCode.Trim(), notification, cancellationToken);
        if (!result.Accepted)
        {
            var code = string.IsNullOrWhiteSpace(result.ErrorCode)
                ? PaymentErrorCodes.Rejected
                : result.ErrorCode!;
            return Result.Failure<ProcessPaymentWebhookResult>(new SemanticError(code));
        }

        return Result.Success(new ProcessPaymentWebhookResult(true, result.Duplicate));
    }
}
