namespace Tooba.Identity.Application.Auth.Models;

/// <summary>Register success payload for HTTP presentation.</summary>
public sealed record AuthRegisterDto(Guid UserId);

/// <summary>Issued session; AccessToken is the opaque session handle, not a JWT.</summary>
public sealed record AuthSessionDto(Guid UserId, Guid SessionId, string AccessToken, string RefreshToken);

/// <summary>Enumeration-safe accepted acknowledgement.</summary>
public sealed record AuthAcceptedDto(bool Accepted);

/// <summary>OTP challenge handle without disclosing account existence.</summary>
public sealed record AuthOtpChallengeDto(bool Accepted, Guid ChallengeId);

/// <summary>Authenticated principal projection for storefront header (not shipping recipient).</summary>
public sealed record AuthMeDto(
    Guid UserId,
    Guid SessionId,
    string Edition,
    string? TenantId,
    string? DisplayName,
    string? FirstName,
    string? LastName,
    string? Mobile);
