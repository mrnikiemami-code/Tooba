using Tooba.Party.Domain;

namespace Tooba.Party.Application;

/// <summary>
/// مرجع پایدار Party برای ماژول‌های دیگر بدون نشت موجودیت EF.
/// </summary>
public sealed record PartyReference(Guid PartyId, PartyKind Kind, string DisplayName);

/// <summary>
/// مرجع سازمان. نوع تجاری واحد را قفل نمی‌کند.
/// </summary>
public sealed record OrganizationReference(Guid PartyId, string DisplayName, string? LegalName);

/// <summary>
/// مرجع عضویت. مجوز SpiceDB داخل آن نیست.
/// </summary>
public sealed record MembershipReference(Guid MembershipId, Guid UserId, Guid PartyId, string RelationCode, MembershipStatus Status);

/// <summary>
/// مرجع پیوند User به Party بدون EF.
/// </summary>
public sealed record UserPartyLinkReference(Guid LinkId, Guid UserId, Guid PartyId);

/// <summary>
/// مرجع رابطهٔ سازمان‌به‌سازمان.
/// </summary>
public sealed record OrganizationRelationshipReference(Guid RelationshipId, Guid FromPartyId, Guid ToPartyId, string RelationCode);

/// <summary>
/// درز خواندن Party برای ماژول‌های دیگر بدون DbContext خارجی.
/// </summary>
public interface IPartyLookupGateway
{
    /// <summary>
    /// Party را در پایگاه Tenant/Marketplace جاری پیدا می‌کند؛ Host parse نمی‌شود.
    /// </summary>
    Task<PartyReference?> FindByIdAsync(Guid partyId, CancellationToken cancellationToken);
}

/// <summary>
/// نوشتن foundation Party. UI تجاری و onboarding فروشنده اینجا نیست.
/// </summary>
public interface IPartyDirectory
{
    /// <summary>
    /// Person کسب‌وکار می‌سازد.
    /// </summary>
    Task<PartyReference> CreatePersonAsync(string displayName, CancellationToken cancellationToken);

    /// <summary>
    /// سازمان می‌سازد.
    /// </summary>
    Task<OrganizationReference> CreateOrganizationAsync(string displayName, string? legalName, CancellationToken cancellationToken);

    /// <summary>
    /// UserId مبهم را به Party وصل می‌کند بدون FK به Identity.
    /// </summary>
    Task<UserPartyLinkReference> LinkUserAsync(Guid userId, Guid partyId, CancellationToken cancellationToken);

    /// <summary>
    /// عضویت را در تراکنش محلی Party می‌نویسد و رویداد تصویرسازی را به Outbox می‌سپارد.
    /// </summary>
    Task<MembershipReference> EstablishMembershipAsync(Guid userId, Guid partyId, string relationCode, CancellationToken cancellationToken);

    /// <summary>
    /// رابطهٔ سازمان‌به‌سازمان گسترش‌پذیر ثبت می‌کند.
    /// </summary>
    Task<OrganizationRelationshipReference> RelateOrganizationsAsync(Guid fromPartyId, Guid toPartyId, string relationCode, CancellationToken cancellationToken);

    /// <summary>
    /// قابلیت تجاری گسترش‌پذیر به سازمان می‌دهد.
    /// </summary>
    Task GrantOrganizationCapabilityAsync(Guid organizationPartyId, string capabilityCode, CancellationToken cancellationToken);

    /// <summary>
    /// پروفایل عملیاتی Organization را می‌خواند؛ برای Person یا نبود Party تهی است.
    /// </summary>
    Task<OrganizationProfileSnapshot?> GetOrganizationProfileAsync(Guid partyId, CancellationToken cancellationToken);

    /// <summary>
    /// پروفایل عملیاتی Organization را به‌روز می‌کند؛ Person رد می‌شود.
    /// </summary>
    Task<OrganizationProfileSnapshot> UpdateOrganizationProfileAsync(
        Guid partyId,
        OrganizationProfileWrite input,
        CancellationToken cancellationToken);
}

/// <summary>
/// نمایهٔ پروفایل عملیاتی سازمان بدون credential ورود.
/// </summary>
public sealed record OrganizationProfileSnapshot(
    Guid PartyId,
    string DisplayName,
    string? LegalName,
    string? Description,
    string? SupportPhone,
    string? SupportEmail,
    string? AddressLine,
    DateTimeOffset UpdatedAt);

/// <summary>
/// ورودی نوشتن پروفایل عملیاتی سازمان.
/// </summary>
public sealed record OrganizationProfileWrite(
    string DisplayName,
    string? LegalName,
    string? Description,
    string? SupportPhone,
    string? SupportEmail,
    string? AddressLine);
