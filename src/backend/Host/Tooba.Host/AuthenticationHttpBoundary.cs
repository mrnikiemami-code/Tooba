using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Tooba.Identity.Application;
using Tooba.Identity.Domain;
using Tooba.Identity.Infrastructure;

namespace Tooba.Host;

/// <summary>
/// اصل احراز همین درخواست HTTP. مجوز کسب‌وکار اینجا حل نمی‌شود و Tenant از هدر/کوئری/بدنه جعل نمی‌شود.
/// </summary>
internal sealed class CurrentAuthenticatedSession
{
    /// <summary>
    /// User پایدار پس از اعتبارسنجی Bearer نشست.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>
    /// شناسهٔ نشست جاری؛ راز Refresh نیست.
    /// </summary>
    public Guid? SessionId { get; private set; }

    /// <summary>
    /// Edition ذخیره‌شده روی نشست، نه از Host درخواست.
    /// </summary>
    public string? Edition { get; private set; }

    /// <summary>
    /// Tenant پایدار نشست در Single-Store؛ از X-Tenant-Id خوانده نمی‌شود.
    /// </summary>
    public string? TenantId { get; private set; }

    /// <summary>
    /// آیا Bearer به نشست زنده و حساب فعال Resolve شده است.
    /// </summary>
    public bool IsAuthenticated => UserId is not null && SessionId is not null;

    /// <summary>
    /// اصل را پس از Resolve نشست می‌نشاند تا لایه‌های بعدی Host از EF Identity نخوانند.
    /// </summary>
    public void Assign(AuthenticatedIdentity identity)
    {
        UserId = identity.UserId;
        SessionId = identity.SessionId;
        Edition = identity.Edition;
        TenantId = identity.TenantId;
    }
}

/// <summary>
/// درز محدودسازی نرخ آینده. هویت را فقط به IP گره نمی‌زند و در این تسک antifraud اجرا نمی‌شود.
/// </summary>
internal interface IAuthenticationThrottleSeam
{
    /// <summary>
    /// رخداد ورود/Refresh/بازنشانی/تأیید را برای محدودساز بعدی علامت می‌زند. راز را نمی‌پذیرد.
    /// </summary>
    void Observe(string operation);
}

/// <summary>
/// پیاده‌سازی خنثی درز نرخ؛ محصول ضد سوءاستفاده نیست.
/// </summary>
internal sealed class NoOpAuthenticationThrottleSeam : IAuthenticationThrottleSeam
{
    /// <inheritdoc />
    public void Observe(string operation)
    {
    }
}

/// <summary>
/// اعتبارسنجی Bearer به‌عنوان SessionId مات. JWT سفارشی ساخته نمی‌شود و هدر Authorization لاگ نمی‌شود.
/// </summary>
internal sealed class SessionAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// میان‌افزار را به pipeline وصل می‌کند.
    /// </summary>
    public SessionAuthenticationMiddleware(RequestDelegate next) => _next = next;

    /// <summary>
    /// نشست زنده را Resolve می‌کند. حساب Disabled/Locked یا مهر ناهماهنگ اصل نمی‌سازد.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, CurrentAuthenticatedSession current)
    {
        var path = context.Request.Path;
        if (path.StartsWithSegments("/health") || path.StartsWithSegments("/ready"))
        {
            await _next(context);
            return;
        }

        if (context.Request.Headers.TryGetValue("Authorization", out var header))
        {
            var raw = header.ToString();
            if (raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                var token = raw["Bearer ".Length..].Trim();
                if (Guid.TryParse(token, out var sessionId))
                {
                    var sessions = context.RequestServices.GetRequiredService<IIdentitySessionResolver>();
                    var identity = await sessions.ResolveAsync(sessionId, context.RequestAborted);
                    if (identity is not null)
                    {
                        current.Assign(identity);
                    }
                }
            }
        }

        await _next(context);
    }
}

/// <summary>
/// قراردادهای JSON مرز احراز. موجودیت EF نیستند و هش/راز persistشده را برنمی‌گردانند مگر Refresh خام در صدور/چرخش.
/// </summary>
internal static class AuthenticationHttpModels
{
    /// <summary>ثبت حساب با شناسهٔ typed.</summary>
    internal sealed class RegisterRequest
    {
        /// <summary>گونهٔ شناسه؛ از Host ساخته نمی‌شود.</summary>
        public string? IdentifierKind { get; init; }

        /// <summary>مقدار خام شناسه.</summary>
        public string? Identifier { get; init; }

        /// <summary>رمز plaintext فقط در حافظهٔ درخواست.</summary>
        public string? Password { get; init; }

        /// <summary>شناسهٔ Tenant اگر کلاینت بفرستد؛ منبع اعتماد نیست و رد می‌شود.</summary>
        public string? TenantId { get; init; }

        /// <summary>فیلدهای ناشناخته برای کشف جعل Tenant در بدنه.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }

    /// <summary>ورود با شناسه و رمز. شکست عمومی enumeration حساب را لو نمی‌دهد.</summary>
    internal sealed class LoginRequest
    {
        /// <summary>گونهٔ شناسه.</summary>
        public string? IdentifierKind { get; init; }

        /// <summary>مقدار خام شناسه.</summary>
        public string? Identifier { get; init; }

        /// <summary>رمز plaintext.</summary>
        public string? Password { get; init; }

        /// <summary>شناسهٔ Tenant جعلی در بدنه.</summary>
        public string? TenantId { get; init; }

        /// <summary>فیلدهای ناشناخته.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }

    /// <summary>چرخش Refresh. راز قبلی پس از موفقیت نامعتبر است.</summary>
    internal sealed class RefreshRequest
    {
        /// <summary>دستهٔ نشست؛ راز Refresh نیست.</summary>
        public Guid SessionId { get; init; }

        /// <summary>راز Refresh خام فقط در این مرز.</summary>
        public string? RefreshToken { get; init; }

        /// <summary>شناسهٔ Tenant جعلی در بدنه.</summary>
        public string? TenantId { get; init; }

        /// <summary>فیلدهای ناشناخته.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }

    /// <summary>درخواست بازنشانی enumeration-safe.</summary>
    internal sealed class ResetRequest
    {
        /// <summary>گونهٔ شناسه.</summary>
        public string? IdentifierKind { get; init; }

        /// <summary>مقدار خام شناسه.</summary>
        public string? Identifier { get; init; }

        /// <summary>شناسهٔ Tenant جعلی در بدنه.</summary>
        public string? TenantId { get; init; }

        /// <summary>فیلدهای ناشناخته.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }

    /// <summary>تکمیل بازنشانی تک‌مصرف.</summary>
    internal sealed class ResetCompleteRequest
    {
        /// <summary>شناسهٔ چالش پایدار.</summary>
        public Guid ChallengeId { get; init; }

        /// <summary>راز یک‌بارمصرف؛ هش persistشده نیست.</summary>
        public string? Secret { get; init; }

        /// <summary>رمز جدید مطابق سیاست پیکربندی.</summary>
        public string? NewPassword { get; init; }

        /// <summary>شناسهٔ Tenant جعلی در بدنه.</summary>
        public string? TenantId { get; init; }

        /// <summary>فیلدهای ناشناخته.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }

    /// <summary>درخواست تأیید شناسه برای اصل احرازشده.</summary>
    internal sealed class VerificationRequest
    {
        /// <summary>گونهٔ شناسه.</summary>
        public string? IdentifierKind { get; init; }

        /// <summary>مقدار خام شناسهٔ متعلق به User جاری.</summary>
        public string? Identifier { get; init; }

        /// <summary>شناسهٔ Tenant جعلی در بدنه.</summary>
        public string? TenantId { get; init; }

        /// <summary>فیلدهای ناشناخته.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }

    /// <summary>تکمیل تأیید با چالش معتبر.</summary>
    internal sealed class VerificationCompleteRequest
    {
        /// <summary>شناسهٔ چالش.</summary>
        public Guid ChallengeId { get; init; }

        /// <summary>راز یک‌بارمصرف.</summary>
        public string? Secret { get; init; }

        /// <summary>شناسهٔ Tenant جعلی در بدنه.</summary>
        public string? TenantId { get; init; }

        /// <summary>فیلدهای ناشناخته.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }

    /// <summary>تغییر رمز فقط با نشست معتبر و رمز جاری.</summary>
    internal sealed class ChangePasswordRequest
    {
        /// <summary>رمز جاری برای اثبات مالکیت.</summary>
        public string? CurrentPassword { get; init; }

        /// <summary>رمز جدید.</summary>
        public string? NewPassword { get; init; }

        /// <summary>شناسهٔ Tenant جعلی در بدنه.</summary>
        public string? TenantId { get; init; }

        /// <summary>فیلدهای ناشناخته.</summary>
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }

    /// <summary>پاسخ ثبت بدون موجودیت EF.</summary>
    internal sealed record RegisterResponse(Guid UserId);

    /// <summary>پاسخ نشست؛ accessToken همان SessionId مات است نه JWT.</summary>
    internal sealed record SessionResponse(Guid UserId, Guid SessionId, string AccessToken, string RefreshToken);

    /// <summary>پاسخ عمومی بازنشانی بدون ChallengeId تا enumeration رخ ندهد.</summary>
    internal sealed record AcceptedResponse(bool Accepted);

    /// <summary>اصل جاری بدون راز، هش، یا SecurityStamp.</summary>
    internal sealed record MeResponse(Guid UserId, Guid SessionId, string Edition, string? TenantId);
}

/// <summary>
/// نگاشت مسیرهای /v1/auth. مرز HTTP است نه دامنه و تماس مجوز اینجا انجام نمی‌شود.
/// </summary>
internal static class AuthenticationEndpointMapper
{
    private static readonly HashSet<string> ForbiddenTenantKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "X-Tenant-Id", "X-TenantId", "TenantId", "tenantId", "tenant_id",
    };

    /// <summary>
    /// مسیرهای احراز نسخهٔ ۱ را ثبت می‌کند. کوکی امن پیش‌فرض ساخته نمی‌شود.
    /// </summary>
    public static void MapAuthenticationBoundary(this WebApplication app)
    {
        var group = app.MapGroup("/v1/auth");
        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        group.MapPost("/refresh", RefreshAsync);
        group.MapPost("/logout", LogoutAsync);
        group.MapPost("/logout-all", LogoutAllAsync);
        group.MapPost("/password-reset/request", RequestResetAsync);
        group.MapPost("/password-reset/complete", CompleteResetAsync);
        group.MapPost("/identifier-verification/request", RequestVerificationAsync);
        group.MapPost("/identifier-verification/complete", CompleteVerificationAsync);
        group.MapPost("/password-change", ChangePasswordAsync);
        group.MapGet("/me", MeAsync);
    }

    private static async Task<IResult> RegisterAsync(
        HttpContext http,
        AuthenticationHttpModels.RegisterRequest body,
        IIdentityAuthenticationService auth,
        ILoggerFactory loggers)
    {
        if (RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        if (!TryParseKind(body.IdentifierKind, out var kind)
            || string.IsNullOrWhiteSpace(body.Identifier)
            || string.IsNullOrWhiteSpace(body.Password))
        {
            return AuthProblem(http, StatusCodes.Status400BadRequest, "Bad Request", "identity.validation.failed");
        }

        try
        {
            var created = await auth.RegisterAsync(
                new RegisterUserCommand { IdentifierKind = kind, Identifier = body.Identifier, Password = body.Password },
                http.RequestAborted);
            loggers.CreateLogger("Tooba.Auth").LogInformation("identity.register.succeeded");
            return Results.Json(new AuthenticationHttpModels.RegisterResponse(created.UserId), statusCode: StatusCodes.Status201Created);
        }
        catch (IdentityDuplicateIdentifierException)
        {
            return AuthProblem(http, StatusCodes.Status409Conflict, "Conflict", "identity.identifier.conflict");
        }
        catch (ArgumentException)
        {
            return AuthProblem(http, StatusCodes.Status400BadRequest, "Bad Request", "identity.validation.failed");
        }
    }

    private static async Task<IResult> LoginAsync(
        HttpContext http,
        AuthenticationHttpModels.LoginRequest body,
        IIdentityAuthenticationService auth,
        IAuthenticationThrottleSeam throttle,
        ILoggerFactory loggers)
    {
        if (RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        throttle.Observe("login");
        if (!TryParseKind(body.IdentifierKind, out var kind))
        {
            return AuthProblem(http, StatusCodes.Status401Unauthorized, "Unauthorized", "identity.authentication.failed");
        }

        var result = await auth.AuthenticateWithPasswordAsync(kind, body.Identifier ?? "", body.Password ?? "", http.RequestAborted);
        if (!result.Succeeded || result.Ticket is null || string.IsNullOrEmpty(result.Ticket.RefreshToken))
        {
            loggers.CreateLogger("Tooba.Auth").LogInformation("identity.login.failed");
            return AuthProblem(http, StatusCodes.Status401Unauthorized, "Unauthorized", "identity.authentication.failed");
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
        if (RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        throttle.Observe("refresh");
        var result = await auth.RefreshSessionAsync(body.SessionId, body.RefreshToken ?? "", http.RequestAborted);
        if (!result.Succeeded || result.Ticket?.RefreshToken is null)
        {
            loggers.CreateLogger("Tooba.Auth").LogInformation("identity.refresh.failed");
            return AuthProblem(http, StatusCodes.Status401Unauthorized, "Unauthorized", "identity.session.invalid");
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
            return AuthProblem(http, StatusCodes.Status401Unauthorized, "Unauthorized", "identity.session.invalid");
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
            return AuthProblem(http, StatusCodes.Status401Unauthorized, "Unauthorized", "identity.session.invalid");
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
        if (RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        throttle.Observe("password_reset_request");
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
        if (RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        throttle.Observe("password_reset_complete");
        var outcome = await lifecycle.CompletePasswordResetAsync(
            body.ChallengeId,
            body.Secret ?? "",
            body.NewPassword ?? "",
            http.RequestAborted);
        if (outcome != ChallengeConsumeOutcome.Succeeded)
        {
            return AuthProblem(http, StatusCodes.Status400BadRequest, "Bad Request", "identity.challenge.invalid");
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
            return AuthProblem(http, StatusCodes.Status401Unauthorized, "Unauthorized", "identity.session.invalid");
        }

        if (RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        throttle.Observe("identifier_verification_request");
        if (!TryParseKind(body.IdentifierKind, out var kind))
        {
            return AuthProblem(http, StatusCodes.Status400BadRequest, "Bad Request", "identity.validation.failed");
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
            return AuthProblem(http, StatusCodes.Status400BadRequest, "Bad Request", "identity.validation.failed");
        }
    }

    private static async Task<IResult> CompleteVerificationAsync(
        HttpContext http,
        AuthenticationHttpModels.VerificationCompleteRequest body,
        IIdentityCredentialLifecycle lifecycle,
        IAuthenticationThrottleSeam throttle)
    {
        if (RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
        {
            return spoof;
        }

        throttle.Observe("identifier_verification_complete");
        var outcome = await lifecycle.CompleteIdentifierVerificationAsync(
            body.ChallengeId,
            body.Secret ?? "",
            http.RequestAborted);
        if (outcome != ChallengeConsumeOutcome.Succeeded)
        {
            return AuthProblem(http, StatusCodes.Status400BadRequest, "Bad Request", "identity.challenge.invalid");
        }

        return Results.NoContent();
    }

    private static async Task<IResult> ChangePasswordAsync(
        HttpContext http,
        AuthenticationHttpModels.ChangePasswordRequest body,
        CurrentAuthenticatedSession current,
        IIdentityAuthenticationService auth)
    {
        if (!current.IsAuthenticated)
        {
            return AuthProblem(http, StatusCodes.Status401Unauthorized, "Unauthorized", "identity.session.invalid");
        }

        if (RejectUntrustedTenant(http, body.TenantId, body.Extra) is { } spoof)
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
            return AuthProblem(http, StatusCodes.Status400BadRequest, "Bad Request", "identity.password.change.failed");
        }
        catch (ArgumentException)
        {
            return AuthProblem(http, StatusCodes.Status400BadRequest, "Bad Request", "identity.validation.failed");
        }
    }

    private static IResult MeAsync(HttpContext http, CurrentAuthenticatedSession current)
    {
        if (!current.IsAuthenticated)
        {
            return AuthProblem(http, StatusCodes.Status401Unauthorized, "Unauthorized", "identity.session.invalid");
        }

        return Results.Json(new AuthenticationHttpModels.MeResponse(
            current.UserId!.Value,
            current.SessionId!.Value,
            current.Edition ?? "",
            current.TenantId));
    }

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

    private static IResult? RejectUntrustedTenant(
        HttpContext http,
        string? bodyTenantId,
        IReadOnlyDictionary<string, JsonElement>? extra)
    {
        if (!string.IsNullOrWhiteSpace(bodyTenantId))
        {
            return AuthProblem(http, StatusCodes.Status400BadRequest, "Bad Request", "identity.tenant.untrusted");
        }

        if (extra is not null)
        {
            foreach (var key in extra.Keys)
            {
                if (ForbiddenTenantKeys.Contains(key))
                {
                    return AuthProblem(http, StatusCodes.Status400BadRequest, "Bad Request", "identity.tenant.untrusted");
                }
            }
        }

        foreach (var key in ForbiddenTenantKeys)
        {
            if (http.Request.Headers.ContainsKey(key))
            {
                return AuthProblem(http, StatusCodes.Status400BadRequest, "Bad Request", "identity.tenant.untrusted");
            }
        }

        if (http.Request.Query.ContainsKey("tenantId") || http.Request.Query.ContainsKey("tenant_id") || http.Request.Query.ContainsKey("TenantId"))
        {
            return AuthProblem(http, StatusCodes.Status400BadRequest, "Bad Request", "identity.tenant.untrusted");
        }

        if (http.Request.Cookies.ContainsKey("tenantId")
            || http.Request.Cookies.ContainsKey("TenantId")
            || http.Request.Cookies.ContainsKey("tenant_id"))
        {
            return AuthProblem(http, StatusCodes.Status400BadRequest, "Bad Request", "identity.tenant.untrusted");
        }

        return null;
    }

    private static IResult AuthProblem(HttpContext http, int status, string title, string errorCode)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? http.TraceIdentifier;
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = "about:blank",
        };
        problem.Extensions["traceId"] = traceId;
        problem.Extensions["errorCode"] = errorCode;
        return Results.Json(problem, statusCode: status, contentType: "application/problem+json");
    }
}
