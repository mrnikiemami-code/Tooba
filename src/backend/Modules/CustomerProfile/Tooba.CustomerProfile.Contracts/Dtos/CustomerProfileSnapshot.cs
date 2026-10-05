namespace Tooba.CustomerProfile.Contracts.Dtos;

/// <summary>Private descriptive profile snapshot without owner/credential projection in API payloads.</summary>
public sealed record CustomerProfileSnapshot(
    string FirstName,
    string LastName,
    string DisplayName,
    string? BirthDate,
    string? Bio,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
