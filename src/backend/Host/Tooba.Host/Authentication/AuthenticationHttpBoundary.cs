using System.Text.Json;
using Tooba.CustomerProfile.Contracts;
using Tooba.Host.Storefront;
using Tooba.Identity.Contracts;
using Tooba.Identity.Contracts.Problems;

namespace Tooba.Host;

/// <summary>
/// نگاشت مسیرهای /v1/auth. مرز HTTP است نه دامنه و تماس مجوز اینجا انجام نمی‌شود.
/// </summary>
internal static class AuthenticationEndpointMapper
{
    /// <summary>
    /// مسیرهای احراز نسخهٔ ۱ را ثبت می‌کند. کوکی امن پیش‌فرض ساخته نمی‌شود.
    /// </summary>
    public static void MapAuthenticationBoundary(this WebApplication app, bool enableCors = false)
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
        AuthenticationHttpModels.RegisterRequest body,
        IIdentityAuthenticationService auth,
        ILoggerFactory loggers)
    {
        if (AuthenticationHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (!TryParseKind(body.IdentifierKind, out var kind)
            || string.IsNullOrWhiteSpace(body.Identifier)
            || string.IsNullOrWhiteSpace(body.Password))
        {
            return AuthenticationHttpProblem.BadRequest(http, IdentityErrorCodes.ValidationFailed);
        }

        try
        {
            var created = await auth.RegisterAsync(
                new RegisterUserCommand { IdentifierKind = kind, Identifier = body.Identifier, Password = body.Password },
                http.RequestAborted);
            loggers.CreateLogger("Tooba.Auth").LogInformation("identity.register.succeeded");
            return Results.Json(new AuthenticationHttpModels.RegisterResponse(created.UserId), statusCode: StatusCodes.Status201Created);
        }
        catch (IdentityDuplicateIdentifierFault)
        {
            return AuthenticationHttpProblem.BadRequest(http, IdentityErrorCodes.IdentifierConflict);
        }
        catch (ArgumentException)
        {
            return AuthenticationHttpProblem.BadRequest(http, IdentityErrorCodes.ValidationFailed);
        }
    }

    private static async Task<IResult> LoginAsync(
        HttpContext http,
        AuthenticationHttpModels.LoginRequest body,
        IIdentityAuthenticationService auth,
        IAuthenticationThrottleSeam throttle,
        ILoggerFactory loggers)
    {
        if (AuthenticationHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (AuthenticationHttpProblem.RejectIfThrottled(http, throttle, "login") is { } throttled)
        {
            return throttled;
        }

        if (!TryParseKind(body.IdentifierKind, out var kind))
        {
            return AuthenticationHttpProblem.Unauthorized(http, IdentityErrorCodes.AuthenticationFailed);
        }

        var result = await auth.AuthenticateWithPasswordAsync(kind, body.Identifier ?? "", body.Password ?? "", http.RequestAborted);
        if (!result.Succeeded || result.Ticket is null || string.IsNullOrEmpty(result.Ticket.RefreshToken))
        {
            loggers.CreateLogger("Tooba.Auth").LogInformation("identity.login.failed");
            return AuthenticationHttpProblem.Unauthorized(http, IdentityErrorCodes.AuthenticationFailed);
        }

        loggers.CreateLogger("Tooba.Auth").LogInformation("identity.login.succeeded");
        return Results.Json(ToSessionResponse(result.Ticket));
    }

    private static async Task<IResult> RefreshAsync(
        HttpContext http,
        AuthenticationHttpModels.RefreshRequest body,
        IIdentityAuthenticationService auth,
        IAuthenticationThrottleSeam throttle,
        ILoggerFactory loggers)
    {
        if (AuthenticationHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (AuthenticationHttpProblem.RejectIfThrottled(http, throttle, "refresh") is { } throttled)
        {
            return throttled;
        }

        var result = await auth.RefreshSessionAsync(body.SessionId, body.RefreshToken ?? "", http.RequestAborted);
        if (!result.Succeeded || result.Ticket?.RefreshToken is null)
        {
            loggers.CreateLogger("Tooba.Auth").LogInformation("identity.refresh.failed");
            return AuthenticationHttpProblem.Unauthorized(http, IdentityErrorCodes.SessionInvalid);
        }

        return Results.Json(ToSessionResponse(result.Ticket));
    }

    private static async Task<IResult> LogoutAsync(
        HttpContext http,
        CurrentAuthenticatedSession current,
        IIdentityAuthenticationService auth)
    {
        if (TryReadBearerSessionId(http, out var sessionId))
        {
            await auth.RevokeSessionAsync(sessionId, "http_logout", http.RequestAborted);
            return Results.NoContent();
        }

        if (!current.IsAuthenticated)
        {
            return AuthenticationHttpProblem.Unauthorized(http, IdentityErrorCodes.SessionInvalid);
        }

        await auth.RevokeSessionAsync(current.SessionId!.Value, "http_logout", http.RequestAborted);
        return Results.NoContent();
    }

    private static async Task<IResult> LogoutAllAsync(
        HttpContext http,
        CurrentAuthenticatedSession current,
        IIdentityAuthenticationService auth)
    {
        if (!current.IsAuthenticated)
        {
            return AuthenticationHttpProblem.Unauthorized(http, IdentityErrorCodes.SessionInvalid);
        }

        await auth.RevokeAllSessionsAsync(current.UserId!.Value, "http_logout_all", http.RequestAborted);
        return Results.NoContent();
    }

    private static async Task<IResult> RequestResetAsync(
        HttpContext http,
        AuthenticationHttpModels.ResetRequest body,
        IIdentityCredentialLifecycle lifecycle,
        IAuthenticationThrottleSeam throttle)
    {
        if (AuthenticationHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (AuthenticationHttpProblem.RejectIfThrottled(http, throttle, "password_reset_request") is { } throttled)
        {
            return throttled;
        }

        if (TryParseKind(body.IdentifierKind, out var kind))
        {
            await lifecycle.RequestPasswordResetAsync(kind, body.Identifier ?? "", http.RequestAborted);
        }

        return Results.Json(new AuthenticationHttpModels.AcceptedResponse(true));
    }

    private static async Task<IResult> CompleteResetAsync(
        HttpContext http,
        AuthenticationHttpModels.ResetCompleteRequest body,
        IIdentityCredentialLifecycle lifecycle,
        IAuthenticationThrottleSeam throttle)
    {
        if (AuthenticationHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (AuthenticationHttpProblem.RejectIfThrottled(http, throttle, "password_reset_complete") is { } throttled)
        {
            return throttled;
        }

        var outcome = await lifecycle.CompletePasswordResetAsync(
            body.ChallengeId,
            body.Secret ?? "",
            body.NewPassword ?? "",
            http.RequestAborted);
        if (outcome != ChallengeConsumeOutcome.Succeeded)
        {
            return AuthenticationHttpProblem.BadRequest(http, IdentityErrorCodes.ChallengeInvalid);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> RequestVerificationAsync(
        HttpContext http,
        AuthenticationHttpModels.VerificationRequest body,
        CurrentAuthenticatedSession current,
        IIdentityCredentialLifecycle lifecycle,
        IAuthenticationThrottleSeam throttle)
    {
        if (!current.IsAuthenticated)
        {
            return AuthenticationHttpProblem.Unauthorized(http, IdentityErrorCodes.SessionInvalid);
        }

        if (AuthenticationHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (AuthenticationHttpProblem.RejectIfThrottled(http, throttle, "identifier_verification_request") is { } throttled)
        {
            return throttled;
        }

        if (!TryParseKind(body.IdentifierKind, out var kind))
        {
            return AuthenticationHttpProblem.BadRequest(http, IdentityErrorCodes.ValidationFailed);
        }

        try
        {
            await lifecycle.IssueIdentifierVerificationAsync(
                current.UserId!.Value,
                kind,
                body.Identifier ?? "",
                http.RequestAborted);
            return Results.Json(new AuthenticationHttpModels.AcceptedResponse(true));
        }
        catch (InvalidOperationException)
        {
            return AuthenticationHttpProblem.BadRequest(http, IdentityErrorCodes.ValidationFailed);
        }
    }

    private static async Task<IResult> CompleteVerificationAsync(
        HttpContext http,
        AuthenticationHttpModels.VerificationCompleteRequest body,
        IIdentityCredentialLifecycle lifecycle,
        IAuthenticationThrottleSeam throttle)
    {
        if (AuthenticationHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (AuthenticationHttpProblem.RejectIfThrottled(http, throttle, "identifier_verification_complete") is { } throttled)
        {
            return throttled;
        }

        var outcome = await lifecycle.CompleteIdentifierVerificationAsync(
            body.ChallengeId,
            body.Secret ?? "",
            http.RequestAborted);
        if (outcome != ChallengeConsumeOutcome.Succeeded)
        {
            return AuthenticationHttpProblem.BadRequest(http, IdentityErrorCodes.ChallengeInvalid);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> RequestOtpLoginAsync(
        HttpContext http,
        AuthenticationHttpModels.OtpLoginRequest body,
        IIdentityOtpLoginService otpLogin,
        IAuthenticationThrottleSeam throttle)
    {
        if (AuthenticationHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (AuthenticationHttpProblem.RejectIfThrottled(http, throttle, "otp_login_request") is { } throttled)
        {
            return throttled;
        }

        try
        {
            var handle = await otpLogin.RequestLoginAsync(body.Identifier ?? "", http.RequestAborted);
            return Results.Json(new { accepted = true, challengeId = handle.ChallengeId });
        }
        catch (ArgumentException)
        {
            return AuthenticationHttpProblem.BadRequest(http, IdentityErrorCodes.ValidationFailed);
        }
        catch (InvalidOperationException)
        {
            return AuthenticationHttpProblem.BadRequest(http, IdentityErrorCodes.OtpDeliveryUnavailable);
        }
    }

    private static async Task<IResult> CompleteOtpLoginAsync(
        HttpContext http,
        AuthenticationHttpModels.OtpLoginCompleteRequest body,
        IIdentityOtpLoginService otpLogin,
        IAuthenticationThrottleSeam throttle,
        ILoggerFactory loggers)
    {
        if (AuthenticationHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (AuthenticationHttpProblem.RejectIfThrottled(http, throttle, "otp_login_complete") is { } throttled)
        {
            return throttled;
        }

        var result = await otpLogin.CompleteLoginAsync(
            body.Identifier ?? "",
            body.ChallengeId,
            body.Secret ?? "",
            http.RequestAborted);
        if (!result.Succeeded || result.Ticket is null || string.IsNullOrEmpty(result.Ticket.RefreshToken))
        {
            loggers.CreateLogger("Tooba.Auth").LogInformation("identity.otp_login.failed");
            return AuthenticationHttpProblem.Unauthorized(http, IdentityErrorCodes.AuthenticationFailed);
        }

        loggers.CreateLogger("Tooba.Auth").LogInformation("identity.otp_login.succeeded");
        return Results.Json(ToSessionResponse(result.Ticket));
    }

    private static async Task<IResult> ChangePasswordAsync(
        HttpContext http,
        AuthenticationHttpModels.ChangePasswordRequest body,
        CurrentAuthenticatedSession current,
        IIdentityAuthenticationService auth)
    {
        if (!current.IsAuthenticated)
        {
            return AuthenticationHttpProblem.Unauthorized(http, IdentityErrorCodes.SessionInvalid);
        }

        if (AuthenticationHttpProblem.RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        try
        {
            await auth.ChangePasswordAsync(
                current.UserId!.Value,
                body.CurrentPassword ?? "",
                body.NewPassword ?? "",
                http.RequestAborted);
            return Results.NoContent();
        }
        catch (InvalidOperationException)
        {
            return AuthenticationHttpProblem.BadRequest(http, IdentityErrorCodes.PasswordChangeFailed);
        }
        catch (ArgumentException)
        {
            return AuthenticationHttpProblem.BadRequest(http, IdentityErrorCodes.ValidationFailed);
        }
    }

    private static async Task<IResult> MeAsync(
        HttpContext http,
        CurrentAuthenticatedSession current,
        ICustomerProfileDirectory profiles,
        IIdentityContactLookup contacts,
        CancellationToken cancellationToken)
    {
        if (!current.IsAuthenticated)
        {
            return AuthenticationHttpProblem.Unauthorized(http, IdentityErrorCodes.SessionInvalid);
        }

        var userId = current.UserId!.Value;
        var profile = await profiles.GetAsync(userId, cancellationToken);
        var contact = await contacts.GetContactAsync(userId, cancellationToken);
        var displayName = StorefrontAccountIdentity.CanonicalName(
            profile?.DisplayName,
            profile?.FirstName,
            profile?.LastName);
        return Results.Json(new AuthenticationHttpModels.MeResponse(
            userId,
            current.SessionId!.Value,
            current.Edition ?? "",
            current.TenantId,
            displayName,
            BlankToNull(profile?.FirstName),
            BlankToNull(profile?.LastName),
            BlankToNull(contact.Mobile)));
    }

    private static string? BlankToNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AuthenticationHttpModels.SessionResponse ToSessionResponse(AuthenticationTicket ticket) =>
        new(ticket.UserId, ticket.SessionHandle, ticket.SessionHandle.ToString("D"), ticket.RefreshToken!);

    private static bool TryParseKind(string? raw, out LoginIdentifierKind kind) =>
        Enum.TryParse(raw, ignoreCase: true, out kind) && Enum.IsDefined(kind);

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
