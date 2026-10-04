namespace Tooba.AccessControl.Application.Validation;

/// <summary>خطای دامنهٔ Access Control با کد پایدار (بدون متن محلی در Exception).</summary>
public sealed class AccessControlException : Exception
{
    /// <summary>استثنا را با کد پایدار می‌سازد؛ پیام فنی = کد.</summary>
    public AccessControlException(string code) : base(code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("error_code_required", nameof(code));
        Code = code.Trim();
    }

    /// <summary>کد پایدار.</summary>
    public string Code { get; }
}