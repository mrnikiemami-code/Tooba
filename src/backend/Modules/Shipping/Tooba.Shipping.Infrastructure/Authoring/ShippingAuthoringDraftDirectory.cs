using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
/// <b>One atomic boundary (revised in TB-190-R1).</b> این توضیح پیش‌تر می‌گفت فقط یک <c>SaveChanges</c>
/// و هیچ تراکنش دست‌سندی وجود ندارد؛ این دیگر درست نیست. ایندکس یکتای
/// <c>(draft_id, group_order)</c> فوری است، بنابراین جابه‌جایی معتبر ترتیب هیچ UPDATE اول مجاز ندارد.
/// مسیر رابطه‌ای دو مرحله را داخل <b>یک</b> تراکنش ماژول‌محور انجام می‌دهد: مرحلهٔ اول ترتیب موقت
/// بدون‌برخورد (<see cref="ShippingAuthoringDraft.StagedOrderBase"/> + ایندکس نهایی) و مرحلهٔ دوم
/// ترتیب نهایی صفرپایه، هر دو با <c>SaveChangesAsync</c> در همان مرز، و فقط <b>یک</b> commit. هر
/// خطا/لغو، کل جایگزینی را برمی‌گرداند و tracker پاک می‌شود تا ویرایش رد‌شده منتشر نشود. ستون
/// <c>revision</c> همچنان concurrency token واقعی است. مسیر غیررابطه‌ای (آزمون InMemory) یک
/// <c>SaveChanges</c> با ترتیب نهایی مستقیم دارد، چون آنجا ایندکس فوری وجود ندارد.
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
            .Include(candidate => candidate.RegionGroups)
            .ThenInclude(group => group.RateRows)
            .Include(candidate => candidate.RegionGroups)
            .ThenInclude(group => group.Titles)
            .Include(candidate => candidate.Translations)
            .Include(candidate => candidate.MethodSettings)
            .Include(candidate => candidate.WeeklySchedule)
            .ThenInclude(schedule => schedule!.Days)
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

        // R190-02: the complete identity/shape plan is proven BEFORE the first Apply/Order/Revision
        // mutation, so a rejected replacement leaves this very aggregate and tracker untouched.
        IReadOnlyList<ShippingAuthoringDraft.RegionGroupAssignment> plan;
        try
        {
            plan = draft.PreflightRegionGroups(regionGroups);
        }
        catch (SemanticException exception)
        {
            return Result.Failure<ShippingAuthoringDraft>(exception.Error);
        }

        // R190-03: group_order is an immediate UNIQUE index, so a valid swap/reorder has no legal first
        // ordinary UPDATE. The relational path stages a temporary non-conflicting order first and only
        // then writes the final zero-based order. Both stages run inside ONE Shipping-owned transaction
        // with one commit, and every failure/cancellation rolls the whole replacement back.
        var relational = context.Database.IsRelational();
        IDbContextTransaction? transaction = null;
        try
        {
            if (relational)
            {
                transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            }

            draft.ApplyRegionGroups(plan, staged: relational, DateTimeOffset.UtcNow);
            await context.SaveChangesAsync(cancellationToken);

            if (relational)
            {
                draft.ApplyFinalRegionGroupOrder();
                await context.SaveChangesAsync(cancellationToken);
                await transaction!.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackAsync(transaction, cancellationToken);
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftRevisionConflict));
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, "authoring_selected_areas"))
        {
            await RollbackAsync(transaction, cancellationToken);
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringRegionSelectionOverlap));
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, "authoring_region_groups"))
        {
            await RollbackAsync(transaction, cancellationToken);
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringRegionPayloadInvalid));
        }
        catch
        {
            await RollbackAsync(transaction, CancellationToken.None);
            throw;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }

        return Result.Success(draft);
    }

    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringDraft>> ReplaceGroupRateRowsAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        Guid groupId,
        long expectedRevision,
        IReadOnlyList<ShippingAuthoringDraft.ShippingAuthoringRateRowInput> rows,
        string trustedCurrencyCode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(rows);

        // Owner + draft + group are always resolved together: a group id alone is never loadable, so a
        // foreign or invented id gets the same safe not-found answer as a missing one.
        var draft = await Scoped(ToDomainOwner(owner))
            .Include(candidate => candidate.RegionGroups)
            .ThenInclude(group => group.RateRows)
            .SingleOrDefaultAsync(candidate => candidate.DraftId == draftId, cancellationToken);
        if (draft is null)
        {
            return Result.Failure<ShippingAuthoringDraft>(new SemanticError(ShippingErrorCodes.AuthoringDraftNotFound));
        }

        if (draft.FindRegionGroup(groupId) is null)
        {
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringRegionGroupNotFound));
        }

        // A stale client is refused before any tracked row is touched; the root revision column is the
        // relational backstop for the race between this check and the UPDATE.
        if (draft.Revision != expectedRevision)
        {
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftRevisionConflict));
        }

        try
        {
            // Preflight happens inside the aggregate, before the first mutation. The row changes and the
            // root revision update share ONE implicit EF SaveChanges transaction: no explicit
            // BeginTransaction is introduced in this path.
            draft.ReplaceGroupRateRows(groupId, rows, trustedCurrencyCode, DateTimeOffset.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The loser keeps no pending edit that a later SaveChanges could publish.
            context.ChangeTracker.Clear();
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftRevisionConflict));
        }
        catch (DbUpdateException exception) when (IsCheckViolation(exception, "authoring_rate_rows"))
        {
            // The table's CHECK constraints mirror the domain guards; a violation is a rejected payload,
            // never a silent rounding of a threshold or fee into a different rule.
            context.ChangeTracker.Clear();
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringRateRowPayloadInvalid));
        }
        catch
        {
            context.ChangeTracker.Clear();
            throw;
        }

        return Result.Success(draft);
    }

    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringDraft>> ReplacePresentationAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        long expectedRevision,
        IReadOnlyList<ShippingAuthoringDraft.MethodTranslationInput> translations,
        IReadOnlyList<ShippingAuthoringDraft.GroupTitleInput> titles,
        IReadOnlySet<Guid> activeLanguageIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(translations);
        ArgumentNullException.ThrowIfNull(titles);
        ArgumentNullException.ThrowIfNull(activeLanguageIds);

        var draft = await Scoped(ToDomainOwner(owner))
            .Include(candidate => candidate.RegionGroups)
            .ThenInclude(group => group.Titles)
            .Include(candidate => candidate.Translations)
            .SingleOrDefaultAsync(candidate => candidate.DraftId == draftId, cancellationToken);
        if (draft is null)
        {
            return Result.Failure<ShippingAuthoringDraft>(new SemanticError(ShippingErrorCodes.AuthoringDraftNotFound));
        }

        // A stale client is refused before any tracked row is touched; the root revision column is the
        // relational backstop shared with the region/rate writers.
        if (draft.Revision != expectedRevision)
        {
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftRevisionConflict));
        }

        try
        {
            // Preflight proves every entry, group membership and language before the first mutation; the
            // selected-language text changes and the root revision share ONE implicit SaveChanges.
            var plan = draft.PreflightPresentation(translations, titles, activeLanguageIds);
            draft.ApplyPresentation(plan, DateTimeOffset.UtcNow);
            if (plan.HasChanges)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftRevisionConflict));
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, "authoring_draft_translations")
                                                    || IsUniqueViolation(exception, "authoring_group_titles"))
        {
            // The composite uniqueness is the relational mirror of a duplicate (owner, language) entry.
            context.ChangeTracker.Clear();
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringPresentationPayloadInvalid));
        }
        catch
        {
            context.ChangeTracker.Clear();
            throw;
        }

        return Result.Success(draft);
    }

    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringDraft>> ReplaceWeeklyScheduleAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        long expectedRevision,
        string? timeZoneId,
        IReadOnlyList<ShippingAuthoringDraft.ScheduleDayInput> days,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(days);

        var draft = await Scoped(ToDomainOwner(owner))
            .Include(candidate => candidate.WeeklySchedule)
            .ThenInclude(schedule => schedule!.Days)
            .SingleOrDefaultAsync(candidate => candidate.DraftId == draftId, cancellationToken);
        if (draft is null)
        {
            return Result.Failure<ShippingAuthoringDraft>(new SemanticError(ShippingErrorCodes.AuthoringDraftNotFound));
        }

        // A stale client is refused before any tracked row is touched; the root revision column is the
        // relational backstop shared with the region/rate/presentation writers.
        if (draft.Revision != expectedRevision)
        {
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftRevisionConflict));
        }

        try
        {
            // The WHOLE week is preflighted before the first mutation, so a bad seventh day leaves this
            // aggregate, its tracker, the revision and every unrelated capability untouched. The
            // schedule header, its day rows and the root revision share ONE implicit SaveChanges.
            var plan = draft.PreflightWeeklySchedule(timeZoneId, days);
            draft.ApplyWeeklySchedule(plan, DateTimeOffset.UtcNow);
            if (plan.HasChanges)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftRevisionConflict));
        }
        catch (DbUpdateException exception) when (IsCheckViolation(exception, "authoring_weekly_schedule_days")
                                                    || IsCheckViolation(exception, "authoring_weekly_schedules"))
        {
            // The tables' CHECK constraints mirror the domain guards; a violation is a rejected payload,
            // never a silent normalization of a weekday, a time pair or a timezone.
            context.ChangeTracker.Clear();
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringScheduleDayInvalid));
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, "authoring_weekly_schedule_days"))
        {
            context.ChangeTracker.Clear();
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringScheduleWeekdaySetInvalid));
        }
        catch
        {
            context.ChangeTracker.Clear();
            throw;
        }

        return Result.Success(draft);
    }

    /// <inheritdoc />
    public async Task<Result<ShippingAuthoringDraft>> ReplaceMethodSettingsAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        long expectedRevision,
        Tooba.Shipping.Contracts.Packaging.ShippingFreightPayerMode? freightPaymentMode,
        Tooba.Shipping.Contracts.Authoring.ShippingAuthoringCalculationMode? calculationMode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var draft = await Scoped(ToDomainOwner(owner))
            .Include(candidate => candidate.MethodSettings)
            .SingleOrDefaultAsync(candidate => candidate.DraftId == draftId, cancellationToken);
        if (draft is null)
        {
            return Result.Failure<ShippingAuthoringDraft>(new SemanticError(ShippingErrorCodes.AuthoringDraftNotFound));
        }

        // A stale client is refused before any tracked row is touched; the root revision column is the
        // relational backstop shared with the region/rate/presentation/schedule writers.
        if (draft.Revision != expectedRevision)
        {
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftRevisionConflict));
        }

        try
        {
            // Both choices are preflighted before the first mutation, so a rejected payload leaves this
            // aggregate, its tracker, its revision and every other capability untouched. The settings row
            // and the root revision share ONE implicit SaveChanges.
            var plan = draft.PreflightMethodSettings(freightPaymentMode, calculationMode);
            draft.ApplyMethodSettings(plan, DateTimeOffset.UtcNow);
            if (plan.HasChanges)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            context.ChangeTracker.Clear();
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftRevisionConflict));
        }
        catch (DbUpdateException exception) when (IsCheckViolation(exception, "authoring_method_settings"))
        {
            // The table's CHECK constraints mirror the enum guards; a violation is a rejected payload,
            // never a silent coercion of an unknown enum value to a defined one.
            context.ChangeTracker.Clear();
            return Result.Failure<ShippingAuthoringDraft>(
                new SemanticError(ShippingErrorCodes.AuthoringMethodSettingsInvalid));
        }
        catch
        {
            context.ChangeTracker.Clear();
            throw;
        }

        return Result.Success(draft);
    }

    /// <summary>
    /// تراکنش را برمی‌گرداند و هیچ تغییر نیمه‌کاره‌ای را در tracker همین context باقی نمی‌گذارد؛
    /// بنابراین یک SaveChanges بعدی در همان context نمی‌تواند ویرایش رد‌شده را منتشر کند.
    /// </summary>
    private async Task RollbackAsync(IDbContextTransaction? transaction, CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            try
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            catch (InvalidOperationException)
            {
                // The provider already rolled the transaction back (e.g. a failed statement).
            }
        }

        context.ChangeTracker.Clear();
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

    /// <summary>
    /// آیا خطای provider یک نقض CHECK همان جدول نام‌برده است. فقط <c>23514</c> روی همان جدول به یک
    /// پاسخ تجاری تبدیل می‌شود؛ هر خطای دیگر provider دوباره پرتاب می‌شود.
    /// </summary>
    private static bool IsCheckViolation(DbUpdateException exception, string tableName)
        => exception.InnerException is PostgresException { SqlState: "23514" } postgres
           && postgres.TableName == tableName;
}
