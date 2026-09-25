using MediatR;
using Tooba.AccessControl.Domain;
using Tooba.Identity.Contracts;
using Tooba.OperatorProfile.Contracts;

using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;
namespace Tooba.AccessControl.Application.Queries.SearchAccessUsers;

/// <summary>
/// پرس‌وجوی کاربران قابل جستجو در محدودهٔ مالک همراه با غنی‌سازی هویت نمایشی.
/// </summary>
/// <param name="OwnerScopeKind">گونهٔ محدودهٔ مالک.</param>
/// <param name="OwnerScopeId">شناسهٔ مالک در محدودهٔ Seller.</param>
/// <param name="TenantId">شناسهٔ Tenant جاری در صورت وجود.</param>
/// <param name="Query">عبارت جستجو در صورت وجود.</param>
public sealed record SearchAccessUsersQuery(
    AccessOwnerScopeKind OwnerScopeKind,
    Guid? OwnerScopeId,
    string? TenantId,
    string? Query) : IRequest<IReadOnlyList<AccessUserHitDto>>;

/// <summary>
/// Handler جست‌وجوی کاربران دسترسی با غنی‌سازی از Contracts ماژول‌های Identity و OperatorProfile.
/// </summary>
public sealed class SearchAccessUsersQueryHandler
    : IRequestHandler<SearchAccessUsersQuery, IReadOnlyList<AccessUserHitDto>>
{
    private readonly IAccessControlDirectory _directory;
    private readonly IActorContactLookup _contacts;
    private readonly IActorDisplayLookup _displays;
    private readonly IActorIdentifierResolver _identifiers;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    /// <param name="contacts">قرارداد تماس Actor (Identity).</param>
    /// <param name="displays">قرارداد نمایش Actor (OperatorProfile).</param>
    /// <param name="identifiers">قرارداد حل شناسهٔ Actor (Identity).</param>
    public SearchAccessUsersQueryHandler(
        IAccessControlDirectory directory,
        IActorContactLookup contacts,
        IActorDisplayLookup displays,
        IActorIdentifierResolver identifiers)
    {
        _directory = directory;
        _contacts = contacts;
        _displays = displays;
        _identifiers = identifiers;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccessUserHitDto>> Handle(
        SearchAccessUsersQuery request, CancellationToken cancellationToken)
    {
        var owner = new AccessOwnerScope(request.OwnerScopeKind, request.OwnerScopeId, request.TenantId);
        var hits = await _directory.SearchUsersInScopeAsync(owner, null, cancellationToken);

        var byUser = hits.ToDictionary(h => h.UserId);
        var q = string.IsNullOrWhiteSpace(request.Query) ? null : request.Query.Trim();

        if (q is not null)
        {
            Guid? resolved = null;
            if (Guid.TryParse(q, out var uid))
            {
                resolved = uid;
            }
            else if (q.Contains('@', StringComparison.Ordinal))
            {
                resolved = await _identifiers.FindUserIdAsync(ActorIdentifierKind.Email, q, cancellationToken);
            }
            else if (q.Any(char.IsDigit) && q.Length >= 8)
            {
                resolved = await _identifiers.FindUserIdAsync(ActorIdentifierKind.Phone, q, cancellationToken);
            }

            if (resolved is { } extraId && !byUser.ContainsKey(extraId))
            {
                byUser[extraId] = new AccessUserHitDto(extraId, Array.Empty<string>());
            }
        }

        var userIds = byUser.Keys.ToList();
        var contactMap = await _contacts.GetActorContactsAsync(userIds, cancellationToken);
        var displayMap = await _displays.GetActorDisplaysAsync(userIds, cancellationToken);

        var enriched = new List<AccessUserHitDto>(byUser.Count);
        foreach (var hit in byUser.Values)
        {
            contactMap.TryGetValue(hit.UserId, out var contact);
            displayMap.TryGetValue(hit.UserId, out var profile);
            var displayName = FirstNonEmpty(
                profile?.DisplayName,
                contact?.Email,
                contact?.Mobile);
            enriched.Add(new AccessUserHitDto(
                hit.UserId,
                hit.RoleCodes,
                displayName,
                contact?.Email,
                contact?.Mobile));
        }

        if (q is null)
        {
            return enriched
                .OrderBy(h => h.DisplayName ?? h.Email ?? h.UserId.ToString("D"), StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return enriched
            .Where(h =>
                (h.DisplayName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (h.Email?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || (h.Mobile?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
                || h.UserId.ToString("D").Contains(q, StringComparison.OrdinalIgnoreCase)
                || h.RoleCodes.Any(c => c.Contains(q, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(h => h.DisplayName ?? h.Email ?? h.UserId.ToString("D"), StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
            {
                return v.Trim();
            }
        }

        return null;
    }
}
