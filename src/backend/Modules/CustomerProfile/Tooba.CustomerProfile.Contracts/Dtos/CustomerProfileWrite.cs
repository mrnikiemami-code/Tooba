namespace Tooba.CustomerProfile.Contracts.Dtos;

/// <summary>
/// Descriptive profile write input. It carries no email/mobile/password/national code authority.
/// </summary>
public sealed record CustomerProfileWrite(
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? BirthDate,
    string? Bio);
