using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace Tooba.Host;

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

    /// <summary>درخواست ورود OTP مشتری با موبایل.</summary>
    internal sealed class OtpLoginRequest
    {
        public string? Identifier { get; init; }
        public string? TenantId { get; init; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? Extra { get; init; }
    }

    /// <summary>تکمیل ورود OTP.</summary>
    internal sealed class OtpLoginCompleteRequest
    {
        public string? Identifier { get; init; }
        public Guid ChallengeId { get; init; }
        public string? Secret { get; init; }
        public string? TenantId { get; init; }

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

    /// <summary>اصل جاری بدون راز، هش، یا SecurityStamp. نام/موبایل برای هدر ویترین است نه هویت ارسال.</summary>
    internal sealed record MeResponse(
        Guid UserId,
        Guid SessionId,
        string Edition,
        string? TenantId,
        string? DisplayName,
        string? FirstName,
        string? LastName,
        string? Mobile);
}

/// <summary>
/// خطاهای مرز احراز به‌صورت ProblemDetails با traceId و errorCode. راز و وجود حساب لو نمی‌رود.
/// </summary>
internal static class AuthenticationHttpProblem
{
    private static readonly HashSet<string> ForbiddenTenantKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "X-Tenant-Id", "X-TenantId", "TenantId", "tenantId", "tenant_id",
    };

    /// <summary>
    /// Tenant جعلی از هدر، کوئری، کوکی، بدنه یا extension-data را رد می‌کند.
    /// </summary>
    public static IResult? RejectUntrustedTenant(
        HttpContext http,
        string? bodyTenantId,
        IReadOnlyDictionary<string, JsonElement>? extra)
    {
        if (!string.IsNullOrWhiteSpace(bodyTenantId))
        {
            return BadRequest(http, "identity.tenant.untrusted");
        }

        if (extra is not null)
        {
            foreach (var key in extra.Keys)
            {
                if (ForbiddenTenantKeys.Contains(key))
                {
                    return BadRequest(http, "identity.tenant.untrusted");
                }
            }
        }

        foreach (var key in ForbiddenTenantKeys)
        {
            if (http.Request.Headers.ContainsKey(key))
            {
                return BadRequest(http, "identity.tenant.untrusted");
            }
        }

        if (http.Request.Query.ContainsKey("tenantId") || http.Request.Query.ContainsKey("tenant_id") || http.Request.Query.ContainsKey("TenantId"))
        {
            return BadRequest(http, "identity.tenant.untrusted");
        }

        if (http.Request.Cookies.ContainsKey("tenantId")
            || http.Request.Cookies.ContainsKey("TenantId")
            || http.Request.Cookies.ContainsKey("tenant_id"))
        {
            return BadRequest(http, "identity.tenant.untrusted");
        }

        return null;
    }

    /// <summary>
    /// اگر permیت این operation در پنجرهٔ جاری تمام شده باشد 429 enumeration-safe برمی‌گرداند.
    /// </summary>
    public static IResult? RejectIfThrottled(HttpContext http, IAuthenticationThrottleSeam throttle, string operation)
    {
        if (throttle.TryAcquire(http, operation))
        {
            return null;
        }

        return AuthProblem(http, StatusCodes.Status429TooManyRequests, "Too Many Requests", "identity.rate_limited");
    }

    /// <summary>ProblemDetails 400 با کد خطای مرز احراز.</summary>
    public static IResult BadRequest(HttpContext http, string errorCode) =>
        AuthProblem(http, StatusCodes.Status400BadRequest, "Bad Request", errorCode);

    /// <summary>ProblemDetails 401 با کد خطای مرز احراز.</summary>
    public static IResult Unauthorized(HttpContext http, string errorCode) =>
        AuthProblem(http, StatusCodes.Status401Unauthorized, "Unauthorized", errorCode);

    /// <summary>ProblemDetails بدون نشت راز؛ traceId از Activity جاری می‌آید.</summary>
    public static IResult AuthProblem(HttpContext http, int status, string title, string errorCode)
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
