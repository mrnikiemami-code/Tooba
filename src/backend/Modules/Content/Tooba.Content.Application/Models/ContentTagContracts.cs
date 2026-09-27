namespace Tooba.Content.Application.Models;

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

/// <summary>فرمان ایجاد برچسب.</summary>
public sealed record CreateContentTagCommand(string LanguageCode, string Name, string? Slug);
