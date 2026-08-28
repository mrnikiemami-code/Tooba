namespace Tooba.UserPreference.Domain;

/// <summary>
/// ترجیح کلیددار UI برای Actor. جدا از locale است؛ payload JSON آزاد است.
/// </summary>
public sealed class UiPreference
{
    /// <summary>حداکثر طول کلید ترجیح.</summary>
    public const int KeyMaxLength = 128;

    private UiPreference()
    {
    }

    /// <summary>شناسهٔ پایدار ردیف.</summary>
    public Guid PreferenceId { get; init; }

    /// <summary>مالک سرورمحور؛ هرگز از بدنهٔ HTTP پذیرفته نمی‌شود.</summary>
    public Guid ActorUserId { get; init; }

    /// <summary>کلید منطقی مثل <c>grid.admin.products</c>.</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>بدنهٔ JSON به‌صورت متن.</summary>
    public string JsonPayload { get; private set; } = "{}";

    /// <summary>زمان آخرین ویرایش UTC.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>ردیف جدید برای Actor و کلید می‌سازد.</summary>
    public static UiPreference Create(Guid actorUserId, string key, string jsonPayload, DateTimeOffset now)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new InvalidOperationException("Actor معتبر الزامی است.");
        }

        var preference = new UiPreference
        {
            PreferenceId = Guid.NewGuid(),
            ActorUserId = actorUserId,
            UpdatedAt = now,
        };
        preference.Apply(key, jsonPayload, now);
        return preference;
    }

    /// <summary>کلید و payload را به‌روز می‌کند.</summary>
    public void Update(string jsonPayload, DateTimeOffset now) => Apply(Key, jsonPayload, now);

    private void Apply(string key, string jsonPayload, DateTimeOffset now)
    {
        Key = NormalizeKey(key);
        if (string.IsNullOrWhiteSpace(jsonPayload))
        {
            throw new InvalidOperationException("JsonPayload خالی مجاز نیست.");
        }

        JsonPayload = jsonPayload;
        UpdatedAt = now;
    }

    /// <summary>کلید را نرمال و اعتبارسنجی می‌کند.</summary>
    public static string NormalizeKey(string? key)
    {
        var trimmed = key?.Trim() ?? string.Empty;
        if (trimmed.Length is 0 or > KeyMaxLength)
        {
            throw new InvalidOperationException("کلید ترجیح UI نامعتبر است.");
        }

        return trimmed;
    }
}
