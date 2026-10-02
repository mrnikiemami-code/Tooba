using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.Identity.Application.Auth.Commands;
using Tooba.Identity.Application.Auth.Queries;
using Tooba.Identity.Endpoints.Errors;

namespace Tooba.Identity.Endpoints.Auth;

/// <summary>
/// نگاشت مسیرهای /v1/auth. مرز HTTP است نه دامنه؛ جریان کسب‌وکار از MediatR می‌گذرد.
/// </summary>
public static class IdentityAuthEndpoints
{
    /// <summary>
    /// مسیرهای احراز نسخهٔ ۱ را ثبت می‌کند. کوکی امن پیش‌فرض ساخته نمی‌شود.
    /// </summary>
    public static void Map(this IEndpointRouteBuilder app, bool enableCors = false)
    {
        var group = app.MapGroup("/v1/auth");
        if (enableCors)
        {
            group.RequireCors("ToobaCors");
        }

        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapPost("/logout", LogoutAsync);
        group.MapPost("/logout-all", LogoutAllAsync);
        group.MapPost("/password-reset/request", RequestResetAsync);
        group.MapPost("/password-reset/complete", CompleteResetAsync);
        group.MapPost("/identifier-verification/request", RequestVerificationAsync);
        group.MapPost("/identifier-verification/complete", CompleteVerificationAsync);
        group.MapPost("/otp-login/request", RequestOtpLoginAsync);
        group.MapPost("/otp-login/complete", CompleteOtpLoginAsync);
        group.MapPost("/password-change", ChangePasswordAsync);
        group.MapGet("/me", MeAsync);
    }

    private static async Task<IResult> RegisterAsync(
        HttpContext http,
        IdentityAuthHttpModels.RegisterRequest body,
        ISender sender,
        ApiResponseFactory api)
    {
        if (IdentityAuthHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        var result = await sender.Send(
            new RegisterAuthUserCommand(body.IdentifierKind, body.Identifier, body.Password),
            http.RequestAborted);
        return result.IsFailure
            ? api.From(result)
            : Results.Json(result.Value, statusCode: StatusCodes.Status201Created);
    }

    private static async Task<IResult> LoginAsync(
        HttpContext http,
        IdentityAuthHttpModels.LoginRequest body,
        ISender sender,
        ApiResponseFactory api,
        IIdentityAuthThrottle throttle)
    {
        if (IdentityAuthHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (IdentityAuthHttpProblem.RejectIfThrottled(http, throttle, "login") is { } throttled)
        {
            return throttled;
        }

        return api.From(await sender.Send(
            new LoginWithPasswordCommand(body.IdentifierKind, body.Identifier, body.Password),
            http.RequestAborted));
    }

    private static async Task<IResult> RefreshAsync(
        HttpContext http,
        IdentityAuthHttpModels.RefreshRequest body,
        ISender sender,
        ApiResponseFactory api,
        IIdentityAuthThrottle throttle)
    {
        if (IdentityAuthHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (IdentityAuthHttpProblem.RejectIfThrottled(http, throttle, "refresh") is { } throttled)
        {
            return throttled;
        }

        return api.From(await sender.Send(
            new RefreshAuthSessionCommand(body.SessionId, body.RefreshToken),
            http.RequestAborted));
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext http,
        IIdentityHttpSession current,
        ISender sender,
        ApiResponseFactory api)
    {
        TryReadBearerSessionId(http, out var bearerSessionId);
        return api.From(await sender.Send(
            new LogoutSessionCommand(
                bearerSessionId == Guid.Empty ? null : bearerSessionId,
                current.SessionId,
                current.IsAuthenticated),
            http.RequestAborted));
    }

    private static async Task<IResult> LogoutAllAsync(
        HttpContext http,
        IIdentityHttpSession current,
        ISender sender,
        ApiResponseFactory api)
    {
        return api.From(await sender.Send(
            new LogoutAllSessionsCommand(current.UserId, current.IsAuthenticated),
            http.RequestAborted));
    }

    private static async Task<IResult> RequestResetAsync(
        HttpContext http,
        IdentityAuthHttpModels.ResetRequest body,
        ISender sender,
        ApiResponseFactory api,
        IIdentityAuthThrottle throttle)
    {
        if (IdentityAuthHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (IdentityAuthHttpProblem.RejectIfThrottled(http, throttle, "password_reset_request") is { } throttled)
        {
            return throttled;
        }

        return api.From(await sender.Send(
            new RequestPasswordResetCommand(body.IdentifierKind, body.Identifier),
            http.RequestAborted));
    }

    private static async Task<IResult> CompleteResetAsync(
        HttpContext http,
        IdentityAuthHttpModels.ResetCompleteRequest body,
        ISender sender,
        ApiResponseFactory api,
        IIdentityAuthThrottle throttle)
    {
        if (IdentityAuthHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (IdentityAuthHttpProblem.RejectIfThrottled(http, throttle, "password_reset_complete") is { } throttled)
        {
            return throttled;
        }

        return api.From(await sender.Send(
            new CompletePasswordResetCommand(body.ChallengeId, body.Secret, body.NewPassword),
            http.RequestAborted));
    }

    private static async Task<IResult> RequestVerificationAsync(
        HttpContext http,
        IdentityAuthHttpModels.VerificationRequest body,
        IIdentityHttpSession current,
        ISender sender,
        ApiResponseFactory api,
        IIdentityAuthThrottle throttle)
    {
        if (IdentityAuthHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (IdentityAuthHttpProblem.RejectIfThrottled(http, throttle, "identifier_verification_request") is { } throttled)
        {
            return throttled;
        }

        return api.From(await sender.Send(
            new RequestIdentifierVerificationCommand(
                current.UserId ?? Guid.Empty,
                current.IsAuthenticated,
                body.IdentifierKind,
                body.Identifier),
            http.RequestAborted));
    }

    private static async Task<IResult> CompleteVerificationAsync(
        HttpContext http,
        IdentityAuthHttpModels.VerificationCompleteRequest body,
        ISender sender,
        ApiResponseFactory api,
        IIdentityAuthThrottle throttle)
    {
        if (IdentityAuthHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (IdentityAuthHttpProblem.RejectIfThrottled(http, throttle, "identifier_verification_complete") is { } throttled)
        {
            return throttled;
        }

        return api.From(await sender.Send(
            new CompleteIdentifierVerificationCommand(body.ChallengeId, body.Secret),
            http.RequestAborted));
    }

    private static async Task<IResult> RequestOtpLoginAsync(
        HttpContext http,
        IdentityAuthHttpModels.OtpLoginRequest body,
        ISender sender,
        ApiResponseFactory api,
        IIdentityAuthThrottle throttle)
    {
        if (IdentityAuthHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (IdentityAuthHttpProblem.RejectIfThrottled(http, throttle, "otp_login_request") is { } throttled)
        {
            return throttled;
        }

        return api.From(await sender.Send(new RequestOtpLoginCommand(body.Identifier), http.RequestAborted));
    }

    private static async Task<IResult> CompleteOtpLoginAsync(
        HttpContext http,
        IdentityAuthHttpModels.OtpLoginCompleteRequest body,
        ISender sender,
        ApiResponseFactory api,
        IIdentityAuthThrottle throttle)
    {
        if (IdentityAuthHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (IdentityAuthHttpProblem.RejectIfThrottled(http, throttle, "otp_login_complete") is { } throttled)
        {
            return throttled;
        }

        return api.From(await sender.Send(
            new CompleteOtpLoginCommand(body.Identifier, body.ChallengeId, body.Secret),
            http.RequestAborted));
    }

    private static async Task<IResult> ChangePasswordAsync(
        HttpContext http,
        IdentityAuthHttpModels.ChangePasswordRequest body,
        IIdentityHttpSession current,
        ISender sender,
        ApiResponseFactory api)
    {
        if (IdentityAuthHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        return api.From(await sender.Send(
            new ChangePasswordCommand(
                current.UserId ?? Guid.Empty,
                current.IsAuthenticated,
                body.CurrentPassword,
                body.NewPassword),
            http.RequestAborted));
    }

    private static async Task<IResult> MeAsync(
        HttpContext http,
        IIdentityHttpSession current,
        ISender sender,
        ApiResponseFactory api,
        CancellationToken cancellationToken)
    {
        return api.From(await sender.Send(
            new GetAuthMeQuery(
                current.UserId ?? Guid.Empty,
                current.SessionId ?? Guid.Empty,
                current.Edition ?? string.Empty,
                current.TenantId,
                current.IsAuthenticated),
            cancellationToken));
    }

    private static bool TryReadBearerSessionId(HttpContext http, out Guid sessionId)
    {
        sessionId = Guid.Empty;
        if (!http.Request.Headers.TryGetValue("Authorization", out var header))
        {
            return false;
        }

        var raw = header.ToString();
        if (!raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return Guid.TryParse(raw["Bearer ".Length..].Trim(), out sessionId);
    }
}
