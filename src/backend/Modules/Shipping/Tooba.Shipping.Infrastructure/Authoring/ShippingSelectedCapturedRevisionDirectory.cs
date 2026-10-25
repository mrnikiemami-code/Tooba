using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Authoring.Ports;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Infrastructure.Persistence;
using ContractVerdict = Tooba.Shipping.Contracts.Rates.ShippingSelectedCaptureVerdict;
using ContractReasons = Tooba.Shipping.Contracts.Rates.ShippingSelectedCaptureVerdictCodes;

namespace Tooba.Shipping.Infrastructure.Authoring;

/// <summary>
/// آداپتور ماندگاری اشاره‌گر انتخاب capture (نوشتن داخلی + خواندن Contracts) روی همان
/// <see cref="ShippingDbContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>یک مرز اتمی.</b> یک <c>SaveChanges</c> برای اشاره‌گر کافی است؛ هیچ تراکنش دستی و هیچ تغییر در
/// guardهای capture (TB-214) وجود ندارد. نسخهٔ ستون، token همزمانی واقعی است و یکتایی مالک+Binding با
/// ایندکس دیتابیس تضمین می‌شود.
/// </para>
/// <para>
/// <b>هدف فقط capture موجود.</b> انتخاب با <c>GetCapturedRevisionAsync</c> دقیق می‌خواند و payload را
/// تأیید می‌کند؛ هرگز capture نمی‌سازد. Binding ذخیره‌شده از payload تأییدشده استخراج می‌شود و اختلاف آن
/// با Binding درخواستی رد می‌شود.
/// </para>
/// <para>
/// <b>کار ذخیره‌نشدهٔ فراخوان.</b> پاک‌سازی/نوشتن خودکار tracker انجام نمی‌شود: کار ذخیره‌نشدهٔ نامرتبط
/// پیش از هر نوشتن رد می‌شود و در مسیر شکست فقط ورودی همین اشاره‌گر جدا می‌شود؛ استثنا و توکن فراخوان
/// عیناً منتشر می‌شوند.
/// </para>
/// </remarks>
public sealed class ShippingSelectedCapturedRevisionDirectory(
    ShippingDbContext context,
    IShippingAuthoringDraftDirectory drafts,
    ICurrentEdition edition,
    ICurrentTenant tenant) : IShippingSelectedCapturedRevisionDirectory, IShippingSelectedCapturedRevisionLookup
{
    /// <inheritdoc />
    public async Task<Result<ShippingSelectedCaptureSelection>> SelectAsync(
        ShippingAuthoringOwnerScope owner,
        string serviceCode,
        string? optionCode,
        Guid draftId,
        long sourceRevision,
        long expectedSelectionVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);

        if (draftId == Guid.Empty
            || sourceRevision <= 0
            || expectedSelectionVersion < 0
            || ShippingSelectedCapturedRevision.CanonicalKey(serviceCode) is null
            || (optionCode is not null && ShippingSelectedCapturedRevision.CanonicalKey(optionCode) is null))
        {
            return Result.Failure<ShippingSelectedCaptureSelection>(
                new SemanticError(ShippingErrorCodes.SelectedCaptureInvalid));
        }

        RequireCleanTracker();

        // The target must already exist as an IMMUTABLE capture of exactly this owner/draft/revision. This
        // path never captures: a missing/foreign draft revision is a refusal, not a new publication.
        var captured = await drafts.GetCapturedRevisionAsync(owner, draftId, sourceRevision, cancellationToken);
        if (!captured.IsSuccess)
        {
            return Result.Failure<ShippingSelectedCaptureSelection>(
                new SemanticError(ShippingErrorCodes.SelectedCaptureSnapshotUnavailable));
        }

        var snapshot = captured.Value;
        ShippingAuthoringRevisionPayload payload;
        try
        {
            // The stored row headers are the authority; a body describing another identity is corrupt.
            payload = snapshot.ReadPayload();
        }
        catch (SemanticException)
        {
            return Result.Failure<ShippingSelectedCaptureSelection>(
                new SemanticError(ShippingErrorCodes.SelectedCaptureSnapshotUnavailable));
        }

        // The persisted binding is DERIVED from the verified capture, never from the caller's declaration:
        // a disagreement means the caller is selecting some other method and is refused before any write.
        var derivedService = ShippingSelectedCapturedRevision.CanonicalKey(payload.ServiceCode);
        var derivedOption = ShippingSelectedCapturedRevision.CanonicalKey(payload.OptionCode);
        if (derivedService is null
            || !string.Equals(derivedService, ShippingSelectedCapturedRevision.CanonicalKey(serviceCode), StringComparison.Ordinal)
            || !string.Equals(derivedOption, ShippingSelectedCapturedRevision.CanonicalKey(optionCode), StringComparison.Ordinal))
        {
            return Result.Failure<ShippingSelectedCaptureSelection>(
                new SemanticError(ShippingErrorCodes.SelectedCaptureBindingMismatch));
        }

        var key = ToDomainOwner(owner);
        var pointer = await LoadPersistedPointerAsync(key, derivedService, derivedOption, cancellationToken);

        if (pointer is null)
        {
            // A pointer that does not exist yet is only creatable from version 0; anything else is stale.
            if (expectedSelectionVersion != 0)
            {
                return Conflict();
            }

            var created = ShippingSelectedCapturedRevision.CreateSelected(
                UuidV7.New(),
                key,
                derivedService,
                derivedOption,
                snapshot.SnapshotId,
                DateTimeOffset.UtcNow);

            context.SelectedCapturedRevisions.Add(created);
            try
            {
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception))
            {
                Detach(created);
                return Conflict();
            }
            catch
            {
                Detach(created);
                throw;
            }

            return Result.Success(new ShippingSelectedCaptureSelection(
                Exists: true,
                ShippingSelectedCaptureState.Selected,
                created.SelectionVersion,
                created.SelectedSnapshotId,
                snapshot.DraftId,
                snapshot.SourceRevision));
        }

        if (pointer.SelectionVersion != expectedSelectionVersion)
        {
            // Stale expected version conflicts even when the target is identical; never last-write-wins.
            return Conflict();
        }

        if (pointer.SelectedSnapshotId == snapshot.SnapshotId)
        {
            // Same target at the current version: a truthful no-op that preserves version and time.
            return Result.Success(new ShippingSelectedCaptureSelection(
                Exists: true,
                pointer.State,
                pointer.SelectionVersion,
                pointer.SelectedSnapshotId,
                snapshot.DraftId,
                snapshot.SourceRevision));
        }

        try
        {
            pointer.Select(snapshot.SnapshotId, DateTimeOffset.UtcNow);
        }
        catch (SemanticException exception)
        {
            // The domain validates the whole next state BEFORE assigning, so a rejection leaves the tracked
            // pointer untouched: nothing to detach, nothing a later SaveChanges could leak. The stable code
            // is returned as a normal refusal instead of escaping the seam.
            return Result.Failure<ShippingSelectedCaptureSelection>(exception.Error);
        }

        var failure = await TrySaveAsync(pointer, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return Result.Success(new ShippingSelectedCaptureSelection(
            Exists: true,
            pointer.State,
            pointer.SelectionVersion,
            pointer.SelectedSnapshotId,
            snapshot.DraftId,
            snapshot.SourceRevision));
    }

    /// <inheritdoc />
    public async Task<Result<ShippingSelectedCaptureSelection>> ClearAsync(
        ShippingAuthoringOwnerScope owner,
        string serviceCode,
        string? optionCode,
        long expectedSelectionVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(owner);

        var serviceKey = ShippingSelectedCapturedRevision.CanonicalKey(serviceCode);
        var optionKey = ShippingSelectedCapturedRevision.CanonicalKey(optionCode);
        if (serviceKey is null || expectedSelectionVersion < 0 || (optionCode is not null && optionKey is null))
        {
            return Result.Failure<ShippingSelectedCaptureSelection>(
                new SemanticError(ShippingErrorCodes.SelectedCaptureInvalid));
        }

        RequireCleanTracker();

        var key = ToDomainOwner(owner);
        var pointer = await LoadPersistedPointerAsync(key, serviceKey, optionKey, cancellationToken);

        if (pointer is null)
        {
            // Clearing an absent pointer is only lawful from version 0; nothing is inserted and the version
            // is never created retroactively.
            return expectedSelectionVersion == 0
                ? Result.Success(new ShippingSelectedCaptureSelection(
                    Exists: false,
                    ShippingSelectedCaptureState.Cleared,
                    SelectionVersion: null,
                    SnapshotId: null,
                    DraftId: null,
                    SourceRevision: null))
                : Conflict();
        }

        if (pointer.SelectionVersion != expectedSelectionVersion)
        {
            return Conflict();
        }

        bool changed;
        try
        {
            changed = pointer.Clear(DateTimeOffset.UtcNow);
        }
        catch (SemanticException exception)
        {
            // Same strong-exception-safety contract as Select: a rejected clear leaves the pointer untouched.
            return Result.Failure<ShippingSelectedCaptureSelection>(exception.Error);
        }

        if (!changed)
        {
            // Already cleared at the current version: a no-op that keeps the row and its version.
            return Result.Success(new ShippingSelectedCaptureSelection(
                Exists: true,
                pointer.State,
                pointer.SelectionVersion,
                SnapshotId: null,
                DraftId: null,
                SourceRevision: null));
        }

        var failure = await TrySaveAsync(pointer, cancellationToken);
        if (failure is not null)
        {
            return failure;
        }

        return Result.Success(new ShippingSelectedCaptureSelection(
            Exists: true,
            pointer.State,
            pointer.SelectionVersion,
            SnapshotId: null,
            DraftId: null,
            SourceRevision: null));
    }

    /// <inheritdoc />
    public async Task<ShippingSelectedCapturedRevisionResult> ReadAsync(
        ShippingSelectedCapturedRevisionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var serviceKey = ShippingSelectedCapturedRevision.CanonicalKey(request.ServiceCode);
        var optionKey = ShippingSelectedCapturedRevision.CanonicalKey(request.OptionCode);
        if (serviceKey is null || (request.OptionCode is not null && optionKey is null))
        {
            return Refused(ContractVerdict.InvalidRequest, ContractReasons.InvalidRequest);
        }

        var owner = ShippingCapturedOwnerScopeResolver.Resolve(
            request.OwnerKind,
            request.OwnerTenantId,
            request.OwnerSellerPartyId,
            edition,
            tenant);
        if (owner is null)
        {
            return Refused(ContractVerdict.OwnerScopeUnavailable, ContractReasons.OwnerScopeUnavailable);
        }

        var key = ToDomainOwner(owner);

        // Read-only and no fallback: exactly the stored pointer for this owner + canonical binding.
        var pointer = await ScopedPointer(key, serviceKey, optionKey)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        if (pointer is null)
        {
            return new ShippingSelectedCapturedRevisionResult(
                ContractVerdict.Absent,
                ReasonCode: null,
                ShippingSelectedCaptureReadState.Absent,
                SelectionVersion: null,
                SnapshotId: null,
                DraftId: null,
                SourceRevision: null,
                FormatVersion: null,
                ServiceCode: null,
                OptionCode: null,
                ShippingTypeId: null,
                Translations: []);
        }

        if (pointer.SelectedSnapshotId is not { } snapshotId)
        {
            return new ShippingSelectedCapturedRevisionResult(
                ContractVerdict.Cleared,
                ReasonCode: null,
                ShippingSelectedCaptureReadState.Cleared,
                pointer.SelectionVersion,
                SnapshotId: null,
                DraftId: null,
                SourceRevision: null,
                FormatVersion: null,
                ServiceCode: null,
                OptionCode: null,
                ShippingTypeId: null,
                Translations: []);
        }

        // The selected row is resolved by its exact identity AND the same owner: another owner's capture,
        // a deleted row or a corrupt body is a refusal, never a "selected method".
        var snapshot = await context.AuthoringRevisionSnapshots
            .AsNoTracking()
            .SingleOrDefaultAsync(
                row => row.SnapshotId == snapshotId
                       && row.OwnerKind == key.Kind
                       && row.OwnerTenantId == key.TenantId
                       && row.OwnerSellerPartyId == key.SellerPartyId,
                cancellationToken);
        if (snapshot is null)
        {
            return Refused(ContractVerdict.SnapshotUnavailable, ContractReasons.SnapshotUnavailable);
        }

        ShippingAuthoringRevisionPayload payload;
        try
        {
            payload = snapshot.ReadPayload();
        }
        catch (SemanticException)
        {
            return Refused(ContractVerdict.SnapshotUnavailable, ContractReasons.SnapshotUnavailable);
        }

        if (!string.Equals(
                ShippingSelectedCapturedRevision.CanonicalKey(payload.ServiceCode),
                pointer.BindingServiceKey,
                StringComparison.Ordinal)
            || !string.Equals(
                ShippingSelectedCapturedRevision.CanonicalKey(payload.OptionCode),
                pointer.BindingOptionKey,
                StringComparison.Ordinal))
        {
            return Refused(ContractVerdict.SnapshotUnavailable, ContractReasons.SnapshotUnavailable);
        }

        return new ShippingSelectedCapturedRevisionResult(
            ContractVerdict.Selected,
            ReasonCode: null,
            ShippingSelectedCaptureReadState.Selected,
            pointer.SelectionVersion,
            snapshot.SnapshotId,
            snapshot.DraftId,
            snapshot.SourceRevision,
            snapshot.FormatVersion,
            payload.ServiceCode,
            payload.OptionCode,
            payload.ShippingTypeId,
            payload.Translations
                .OrderBy(translation => translation.LanguageId)
                .Select(translation => new ShippingSelectedCapturedTranslation(
                    translation.LanguageId,
                    translation.Name,
                    translation.Description))
                .ToList());
    }

    /// <summary>
    /// یک <c>SaveChanges</c> برای همین اشاره‌گر؛ تعارض همزمانی/یکتایی به کد پایدار تبدیل می‌شود و هر
    /// خطای provider دیگر دوباره پرتاب می‌شود. در شکست فقط ورودی همین اشاره‌گر جدا می‌شود.
    /// </summary>
    /// <param name="pointer">اشاره‌گر تلاش‌شدهٔ همین متد.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    private async Task<Result<ShippingSelectedCaptureSelection>?> TrySaveAsync(
        ShippingSelectedCapturedRevision pointer,
        CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return null;
        }
        catch (DbUpdateConcurrencyException)
        {
            Detach(pointer);
            return Conflict();
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            Detach(pointer);
            return Conflict();
        }
        catch
        {
            // Failure/cancellation: the attempted pointer never stays pending for a later SaveChanges; the
            // caller's tracked entities, exception and token identity are preserved by the plain rethrow.
            Detach(pointer);
            throw;
        }
    }

    /// <summary>
    /// کار ذخیره‌نشدهٔ نامرتبط فراخوان پیش از نوشتن رد می‌شود؛ این درز هرگز آن را flush یا پاک نمی‌کند.
    /// </summary>
    private void RequireCleanTracker()
    {
        if (context.ChangeTracker.HasChanges())
        {
            throw new InvalidOperationException(
                "Shipping selected capture selection requires a clean change tracker; the caller has unsaved work.");
        }
    }

    private static Result<ShippingSelectedCaptureSelection> Conflict()
        => Result.Failure<ShippingSelectedCaptureSelection>(
            new SemanticError(ShippingErrorCodes.SelectedCaptureConflict));

    private static ShippingSelectedCapturedRevisionResult Refused(ContractVerdict verdict, string reasonCode)
        => new(
            verdict,
            reasonCode,
            ShippingSelectedCaptureReadState.Absent,
            SelectionVersion: null,
            SnapshotId: null,
            DraftId: null,
            SourceRevision: null,
            FormatVersion: null,
            ServiceCode: null,
            OptionCode: null,
            ShippingTypeId: null,
            Translations: []);

    private IQueryable<ShippingSelectedCapturedRevision> ScopedPointer(
        ShippingDraftOwner key,
        string serviceKey,
        string? optionKey)
        => context.SelectedCapturedRevisions.Where(row =>
            row.OwnerKind == key.Kind
            && row.OwnerTenantId == key.TenantId
            && row.OwnerSellerPartyId == key.SellerPartyId
            && row.BindingServiceKey == serviceKey
            && row.BindingOptionKey == optionKey);

    /// <summary>
    /// اشاره‌گر <b>یک</b> مالک+Binding را با وضعیت واقعی دیتابیس می‌خواند، پیش از هر تصمیم نسخه/no-op.
    /// </summary>
    /// <remarks>
    /// <para>
    /// صرفاً «تراکر تمیز است» کافی نیست: identity map می‌تواند یک نمونهٔ <b>کهنهٔ تمیز</b> برگرداند و تصمیم
    /// no-op/نسخه را از توکن قدیمی بگیرد. بنابراین همان یک اشاره‌گر صریحاً reload می‌شود؛ فقط همین ورودی
    /// لمس می‌شود، هویت شیء حفظ می‌ماند و هیچ حالت دیگرِ فراخوان پاک/جدا نمی‌شود.
    /// </para>
    /// <para>
    /// ردیفی که بین query و reload حذف شود «غایب» است، نه توکن قابل استفاده؛ در آن حالت فقط همان ورودی
    /// جداشده و مسیر ساخت/تعارض معمول اجرا می‌شود.
    /// </para>
    /// </remarks>
    /// <param name="key">مالک ماندگاری.</param>
    /// <param name="serviceKey">کلید متعارف سرویس.</param>
    /// <param name="optionKey">کلید متعارف گزینه یا تهی.</param>
    /// <param name="cancellationToken">توکن لغو.</param>
    private async Task<ShippingSelectedCapturedRevision?> LoadPersistedPointerAsync(
        ShippingDraftOwner key,
        string serviceKey,
        string? optionKey,
        CancellationToken cancellationToken)
    {
        var tracked = await ScopedPointer(key, serviceKey, optionKey).SingleOrDefaultAsync(cancellationToken);
        if (tracked is null)
        {
            DetachVanishedPointer(key, serviceKey, optionKey);
            return null;
        }

        try
        {
            await context.Entry(tracked).ReloadAsync(cancellationToken);
            return tracked;
        }
        catch (DbUpdateConcurrencyException)
        {
            // The row disappeared between the query and the reload: it is absent, never a usable token.
            Detach(tracked);
            return null;
        }
    }

    /// <summary>
    /// نمونهٔ کهنه‌ای که دیگر ردیفی در دیتابیس ندارد از تراکر جدا می‌شود تا هرگز به‌عنوان توکن زنده استفاده
    /// نشود؛ فقط همین کلید، و فقط وقتی query تأیید کرده ردیفی نیست.
    /// </summary>
    /// <param name="key">مالک ماندگاری.</param>
    /// <param name="serviceKey">کلید متعارف سرویس.</param>
    /// <param name="optionKey">کلید متعارف گزینه یا تهی.</param>
    private void DetachVanishedPointer(ShippingDraftOwner key, string serviceKey, string? optionKey)
    {
        foreach (var entry in context.ChangeTracker.Entries<ShippingSelectedCapturedRevision>().ToList())
        {
            var row = entry.Entity;
            if (row.OwnerKind == key.Kind
                && row.OwnerTenantId == key.TenantId
                && row.OwnerSellerPartyId == key.SellerPartyId
                && row.BindingServiceKey == serviceKey
                && row.BindingOptionKey == optionKey)
            {
                entry.State = EntityState.Detached;
            }
        }
    }

    /// <summary>فقط همان یکتایی نام‌بردهٔ همین جدول «تعارض» است؛ هر خطای دیگر provider منتشر می‌شود.</summary>
    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is PostgresException { SqlState: "23505" } postgres
           && string.Equals(
               postgres.ConstraintName,
               "ux_selected_captured_revisions_owner_binding",
               StringComparison.Ordinal)
           && string.Equals(postgres.TableName, "selected_captured_revisions", StringComparison.Ordinal)
           && (postgres.SchemaName is null
               || string.Equals(postgres.SchemaName, "shipping", StringComparison.Ordinal));

    private void Detach(ShippingSelectedCapturedRevision pointer)
    {
        var entry = context.Entry(pointer);
        if (entry.State != EntityState.Detached)
        {
            entry.State = EntityState.Detached;
        }
    }

    /// <summary>
    /// scope سرورساختهٔ Shipping را به بازنمایی ماندگاری مالک نگاشت می‌کند؛ تنها نقطهٔ نگاشت در این آداپتور.
    /// </summary>
    /// <param name="owner">scope مالک مجاز.</param>
    private static ShippingDraftOwner ToDomainOwner(ShippingAuthoringOwnerScope owner) => owner.Kind switch
    {
        ShippingAuthoringOwnerKind.Store when owner.TenantId is { } tenantId =>
            ShippingDraftOwner.ForStore(tenantId),
        ShippingAuthoringOwnerKind.Marketplace when owner.TenantId is null && owner.SellerPartyId is null =>
            ShippingDraftOwner.ForMarketplace(),
        ShippingAuthoringOwnerKind.Seller when owner.TenantId is null && owner.SellerPartyId is { } seller =>
            ShippingDraftOwner.ForSeller(seller),
        _ => throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringOwnerScopeUnavailable)),
    };
}
