#pragma warning disable CS1591
using Tooba.BuildingBlocks;

namespace Tooba.Catalog.Domain;

/// <summary>بخش Landing متعلق به یک StoreLandingPage. HTML/CSS/JS اجرایی ندارد.</summary>
public sealed class StoreLandingPageSection
{
    private StoreLandingPageSection()
    {
    }

    public Guid PageSectionId { get; init; }

    public Guid PageId { get; init; }

    public string SectionType { get; private set; } = string.Empty;

    public int SortOrder { get; private set; }

    public bool IsEnabled { get; private set; } = true;

    public string ConfigurationJson { get; private set; } = "{}";

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static StoreLandingPageSection Create(
        Guid pageId,
        string? sectionType,
        string? configurationJson,
        int sortOrder,
        DateTimeOffset now)
    {
        var type = StoreLandingPageSectionRegistry.NormalizeType(sectionType);
        if (!StoreLandingPageSectionRegistry.IsApproved(type))
        {
            throw new PlatformHttpException(400, "نوع بخش تأییدشده نیست.", "landing.section.type.invalid");
        }

        var config = StoreLandingPageSectionConfig.ValidateAndNormalize(type, configurationJson);
        return new StoreLandingPageSection
        {
            PageSectionId = UuidV7.New(),
            PageId = pageId,
            SectionType = type,
            SortOrder = sortOrder,
            IsEnabled = true,
            ConfigurationJson = config,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    public void UpdateConfig(string? configurationJson, DateTimeOffset now)
    {
        ConfigurationJson = StoreLandingPageSectionConfig.ValidateAndNormalize(SectionType, configurationJson);
        UpdatedAt = now;
    }

    public void SetEnabled(bool enabled, DateTimeOffset now)
    {
        IsEnabled = enabled;
        UpdatedAt = now;
    }

    public void SetSortOrder(int sortOrder, DateTimeOffset now)
    {
        SortOrder = sortOrder;
        UpdatedAt = now;
    }
}
