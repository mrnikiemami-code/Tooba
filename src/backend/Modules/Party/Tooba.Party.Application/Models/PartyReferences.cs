using Tooba.Party.Domain.Enums;

namespace Tooba.Party.Application.Models;

/// <summary>مرجع پایدار Party برای ماژول‌های دیگر بدون نشت موجودیت EF.</summary>
public sealed record PartyReference(Guid PartyId, PartyKind Kind, string DisplayName);

/// <summary>مرجع سازمان. نوع تجاری واحد را قفل نمی‌کند.</summary>
public sealed record OrganizationReference(Guid PartyId, string DisplayName, string? LegalName);

/// <summary>مرجع عضویت. مجوز SpiceDB داخل آن نیست.</summary>
public sealed record MembershipReference(Guid MembershipId, Guid UserId, Guid PartyId, string RelationCode, MembershipStatus Status);

/// <summary>مرجع پیوند User به Party بدون EF.</summary>
public sealed record UserPartyLinkReference(Guid LinkId, Guid UserId, Guid PartyId);

/// <summary>مرجع رابطهٔ سازمان‌به‌سازمان.</summary>
public sealed record OrganizationRelationshipReference(Guid RelationshipId, Guid FromPartyId, Guid ToPartyId, string RelationCode);

/// <summary>نمایهٔ پروفایل عملیاتی سازمان بدون credential ورود.</summary>
public sealed record OrganizationProfileSnapshot(
    Guid PartyId,
    string DisplayName,
    string? LegalName,
    string? Description,
    string? SupportPhone,
    string? SupportEmail,
    string? AddressLine,
    DateTimeOffset UpdatedAt);

/// <summary>ورودی نوشتن پروفایل عملیاتی سازمان.</summary>
public sealed record OrganizationProfileWrite(
    string DisplayName,
    string? LegalName,
    string? Description,
    string? SupportPhone,
    string? SupportEmail,
    string? AddressLine);
