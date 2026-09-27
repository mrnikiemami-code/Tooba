namespace Tooba.Content.Application.Tags.Models;

/// <summary>نمای Admin برچسب محتوا.</summary>
public sealed record ContentTagDto(
    Guid TagId,
    string LanguageCode,
    string Name,
    string NormalizedName,
    string? Slug,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
