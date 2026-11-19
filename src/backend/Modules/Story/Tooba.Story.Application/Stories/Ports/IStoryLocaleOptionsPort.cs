namespace Tooba.Story.Application.Stories.Ports;

/// <summary>گزینه‌های زبان فعال برای ویرایشگر استوری (فقط خواندن از رجیستری).</summary>
public interface IStoryLocaleOptionsPort
{
    /// <summary>فهرست زبان‌های فعال رجیستری را برمی‌گرداند.</summary>
    Task<IReadOnlyList<StoryLocaleOption>> ListEnabledAsync(CancellationToken cancellationToken);
}

/// <summary>یک گزینهٔ زبان فعال از رجیستری Localization.</summary>
public sealed record StoryLocaleOption(string Code, string Name, bool IsDefault);
