namespace Tooba.OperatorProfile.Application.Models;

/// <summary>Admin HTTP response shape for operator profile (nullable timestamps when absent).</summary>
public sealed record OperatorProfileAdminResponse(
    string FirstName,
    string LastName,
    string DisplayName,
    string? Bio,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    /// <summary>Empty profile payload matching legacy GET-when-missing JSON.</summary>
    public static OperatorProfileAdminResponse Empty { get; } = new("", "", "", null, null, null);

    /// <summary>Maps a directory snapshot to the Admin response shape.</summary>
    public static OperatorProfileAdminResponse FromSnapshot(OperatorProfileSnapshot snapshot) => new(
        snapshot.FirstName,
        snapshot.LastName,
        snapshot.DisplayName,
        snapshot.Bio,
        snapshot.CreatedAt,
        snapshot.UpdatedAt);
}
