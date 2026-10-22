using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;

namespace Tooba.Shipping.Infrastructure.Authoring;

/// <summary>
/// ماندگاری پیش‌نویس‌های نویسندگی روی <see cref="ShippingDbContext"/> خودِ همین ماژول. هیچ DbContext
/// ماژول دیگری خوانده/نوشته نمی‌شود و هیچ FK میان‌ماژولی ساخته نمی‌شود.
/// </summary>
/// <remarks>
/// <para>
/// <b>Owner filter, not trust.</b> هر مسیر خواندن/نوشتن با <b>کل</b> scope مالک فیلتر می‌شود
/// (از جمله Tenant فروشگاه)؛ بارگذاری با شناسه و سپس اعتماد به فراخوان ممنوع است. شناسهٔ مالک
/// دیگر همان «پیدا نشد» ایمن (404) را می‌گیرد.
/// </para>
/// <para>
/// <b>One atomic boundary.</b> جایگزینی کل درخت در یک <c>SaveChangesAsync</c> انجام می‌شود و ستون
/// <c>revision</c> یک concurrency token واقعی است؛ بنابراین UPDATE فقط وقتی اثر دارد که بازبینی
/// ذخیره‌شده همان بازبینی مورد انتظار باشد. هیچ تراکنش دست‌ساز یا چندنوشتنی از سمت مرورگر لازم نیست.
/// </para>
/// </remarks>
public sealed class ShippingAuthoringDraftDirectory(ShippingDbContext context) : IShippingAuthoringDraftDirectory
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<ShippingAuthoringDraft>>> ListAsync(
        ShippingAuthoringOwnerScope owner,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var key = ToDomainOwner(owner);
        var drafts = await Scoped(key)
            .AsNoTracking()
            .Include(draft => draft.RegionGroups)
            .ThenInclude(group => group.Areas)
            .OrderBy(draft => draft.ServiceCode)
            .ThenBy(draft => draft.OptionCode)
            .ThenBy(draft => draft.DraftId)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ShippingAuthoringDraft>>(drafts);
    }

    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringDraft>> GetAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var draft = await Scoped(ToDomainOwner(owner))
            .AsNoTracking()
            .Include(candidate => candidate.RegionGroups)
            .ThenInclude(group => group.Areas)
            .SingleOrDefaultAsync(candidate => candidate.DraftId == draftId, cancellationToken);
        return draft is null
            ? Result.Failure<ShippingAuthoringDraft>(new SemanticError(ShippingErrorCodes.AuthoringDraftNotFound))
            : Result.Success(draft);
    }

    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringDraft>> CreateAsync(
        ShippingAuthoringOwnerScope owner,
        string serviceCode,
        string? optionCode,
        IReadOnlyList<ShippingAuthoringRegionGroupInput> regionGroups,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(regionGroups);

        var key = ToDomainOwner(owner);
        var normalizedCode = serviceCode?.Trim() ?? string.Empty;
        var normalizedOption = string.IsNullOrWhiteSpace(optionCode) ? null : optionCode.Trim();

        // A second draft for the same owner+binding would make "which draft is the method" depend on
        // row order. The pre-check returns a clear business answer and the unique index below is the
        // real invariant for every other writer.
        var exists = await Scoped(key)
            .AnyAsync(
                draft => draft.ServiceCode == normalizedCode && draft.OptionCode == normalizedOption,
                cancellationToken);
        if (exists)
        {
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftBindingDuplicate));
        }

        var draft = ShippingAuthoringDraft.Create(key, normalizedCode, normalizedOption, regionGroups, DateTimeOffset.UtcNow);
        context.AuthoringDrafts.Add(draft);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, "authoring_drafts_owner_binding"))
        {
            context.Entry(draft).State = EntityState.Detached;
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftBindingDuplicate));
        }

        return Result.Success(draft);
    }

    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringDraft>> ReplaceRegionGroupsAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        long expectedRevision,
        IReadOnlyList<ShippingAuthoringRegionGroupInput> regionGroups,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(regionGroups);

        var key = ToDomainOwner(owner);
        var draft = await Scoped(key)
            .Include(candidate => candidate.RegionGroups)
            .ThenInclude(group => group.Areas)
            .SingleOrDefaultAsync(candidate => candidate.DraftId == draftId, cancellationToken);
        if (draft is null)
        {
            return Result.Failure<ShippingAuthoringDraft>(new SemanticError(ShippingErrorCodes.AuthoringDraftNotFound));
        }

        // A stale client is refused before any tracked row is touched; the concurrency token below
        // is the relational backstop for the race between this check and the UPDATE.
        if (draft.Revision != expectedRevision)
        {
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftRevisionConflict));
        }

        try
        {
            draft.ReplaceRegionGroups(regionGroups, DateTimeOffset.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftRevisionConflict));
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, "authoring_selected_areas"))
        {
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringRegionSelectionOverlap));
        }

        return Result.Success(draft);
    }

    private IQueryable<ShippingAuthoringDraft> Scoped(ShippingDraftOwner key)
        => context.AuthoringDrafts.Where(draft =>
            draft.OwnerKind == key.Kind
            && draft.OwnerTenantId == key.TenantId
            && draft.OwnerSellerPartyId == key.SellerPartyId);

    /// <summary>
    /// scope سرورساختهٔ TB-189 را به بازنمایی ماندگاری مالک نگاشت می‌کند. این تنها نقطهٔ نگاشت است؛
    /// Application و Domain هرگز مالک را از transport نمی‌سازند.
    /// </summary>
    private static ShippingDraftOwner ToDomainOwner(ShippingAuthoringOwnerScope owner) => owner.Kind switch
    {
        ShippingAuthoringOwnerKind.Store when owner.TenantId is { } tenant =>
            ShippingDraftOwner.ForStore(tenant),
        ShippingAuthoringOwnerKind.Marketplace when owner.TenantId is null && owner.SellerPartyId is null =>
            ShippingDraftOwner.ForMarketplace(),
        ShippingAuthoringOwnerKind.Seller when owner.TenantId is null && owner.SellerPartyId is { } seller =>
            ShippingDraftOwner.ForSeller(seller),
        _ => throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringOwnerScopeUnavailable)),
    };

    /// <summary>
    /// فقط یک نقض یکتایی PostgreSQL روی همان قید نام‌برده «تکراری» است؛ هر DbUpdateException دیگر
    /// دوباره پرتاب می‌شود تا خطای provider هرگز به یک پاسخ تجاری تبدیل نشود.
    /// </summary>
    private static bool IsUniqueViolation(DbUpdateException exception, string constraintFragment)
        => exception.InnerException is PostgresException { SqlState: "23505" } postgres
           && postgres.ConstraintName?.Contains(constraintFragment, StringComparison.Ordinal) == true;
}
