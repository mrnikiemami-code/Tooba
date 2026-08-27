namespace Tooba.OperatorProfile.Domain;

/// <summary>
/// پروفایل توصیفی خصوصی اپراتور Admin. شناسه‌های ورود در Identity می‌مانند
/// و این aggregate جای تنظیمات سراسری platform را نمی‌گیرد.
/// </summary>
public sealed class OperatorProfile
{
    /// <summary>حداقل طول نام نمایشی.</summary>
    public const int DisplayNameMinLength = 3;

    /// <summary>حداکثر طول نام نمایشی.</summary>
    public const int DisplayNameMaxLength = 128;

    /// <summary>حداکثر طول نام/نام‌خانوادگی.</summary>
    public const int NamePartMaxLength = 64;

    /// <summary>حداکثر طول بیوگرافی.</summary>
    public const int BioMaxLength = 200;

    private OperatorProfile()
    {
    }

    /// <summary>مالک سرورمحور؛ کلید اصلی و هرگز از بدنهٔ HTTP پذیرفته نمی‌شود.</summary>
    public Guid OwnerUserId { get; init; }

    /// <summary>نام کوچک اختیاری.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>نام خانوادگی اختیاری.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>نام نمایشی که UI اپراتور از آن استفاده می‌کند.</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>بیوگرافی اختیاری.</summary>
    public string? Bio { get; private set; }

    /// <summary>زمان ایجاد UTC.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>پروفایل جدید برای Actor مشخص می‌سازد.</summary>
    public static OperatorProfile Create(
        Guid ownerUserId,
        string displayName,
        string? firstName,
        string? lastName,
        string? bio,
        DateTimeOffset now)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Actor معتبر الزامی است.");
        }

        var profile = new OperatorProfile
        {
            OwnerUserId = ownerUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        profile.ApplyFields(displayName, firstName, lastName, bio, now);
        return profile;
    }

    /// <summary>فیلدهای توصیفی مجاز را به‌روز می‌کند.</summary>
    public void Update(
        string displayName,
        string? firstName,
        string? lastName,
        string? bio,
        DateTimeOffset now) =>
        ApplyFields(displayName, firstName, lastName, bio, now);

    private void ApplyFields(
        string displayName,
        string? firstName,
        string? lastName,
        string? bio,
        DateTimeOffset now)
    {
        DisplayName = RequireBounded(displayName, DisplayNameMinLength, DisplayNameMaxLength, "نام نمایشی معتبر نیست.");
        FirstName = OptionalBounded(firstName, NamePartMaxLength, "نام بیش از حد بلند است.")
            ?? DeriveFirstName(DisplayName);
        LastName = OptionalBounded(lastName, NamePartMaxLength, "نام خانوادگی بیش از حد بلند است.")
            ?? DeriveLastName(DisplayName);
        Bio = OptionalBounded(bio, BioMaxLength, "بیوگرافی بیش از حد بلند است.");
        UpdatedAt = now;
    }

    private static string DeriveFirstName(string displayName)
    {
        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length == 0 ? displayName : parts[0];
    }

    private static string DeriveLastName(string displayName)
    {
        var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length <= 1 ? string.Empty : string.Join(' ', parts.Skip(1));
    }

    private static string RequireBounded(string? value, int min, int max, string message)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length < min || trimmed.Length > max)
        {
            throw new InvalidOperationException(message);
        }

        return trimmed;
    }

    private static string? OptionalBounded(string? value, int max, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > max)
        {
            throw new InvalidOperationException(message);
        }

        return trimmed;
    }
}
