using System.Text.Json;
using System.Text.Json.Serialization;

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
