#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Application.Storefront.Shipping.CapturedPreview;
using Tooba.Order.Domain;
using Tooba.Order.Domain.Checkout;
using Tooba.Order.Infrastructure.Persistence;

namespace Tooba.Order.Infrastructure.Storefront;

public sealed class StorefrontShippingDraftStore(OrderDbContext db) : IStorefrontShippingDraftStore
{
    public Task<CartShippingDraft?> GetByCartIdAsync(Guid cartId, CancellationToken cancellationToken) =>
        db.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .FirstOrDefaultAsync(x => x.CartId == cartId, cancellationToken);

    public async Task SaveAsync(CartShippingDraft draft, bool isNew, CancellationToken cancellationToken)
    {
        if (isNew)
        {
            db.ShippingDrafts.Add(draft);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CartShippingDraft?> GetForCapturedSelectionAsync(Guid cartId, CancellationToken cancellationToken)
    {
        // کار در انتظارِ نامرتبط هرگز flush/پاک نمی‌شود: نوشتن پیش از هر خواندن/تغییر/تراکنش رد می‌شود.
        EnsureNoForeignPendingWork();

        // حقیقت ذخیره‌شده مرجع است، نه identity map: هر نمونهٔ tracked همین سبد (که ممکن است کهنه
        // باشد) کنار گذاشته می‌شود و گراف تازه و detached خوانده می‌شود.
        foreach (var previous in db.ChangeTracker.Entries<CartShippingDraft>()
                     .Where(entry => entry.Entity.CartId == cartId)
                     .Select(entry => entry.Entity)
                     .ToArray())
        {
            DetachDraftGraph(previous);
        }

        return await db.ShippingDrafts
            .AsNoTracking()
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .FirstOrDefaultAsync(x => x.CartId == cartId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CartShippingDraft> ReplaceCapturedSelectionAsync(
        CartShippingDraft draft,
        IReadOnlyList<CartShippingDraftGroup> groups,
        bool isNew,
        long expectedSelectionRevision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(groups);

        // پیش‌شرط: context برای همین نوشتن تمیز است؛ کار در انتظار فراخوان دست‌نخورده رد می‌شود.
        EnsureNoForeignPendingWork();

        // TB-232-R2: تراکنشِ بازِ فراخوان مالکیت داراست؛ نه adopt می‌شود و نه تودرتو. پیش از هر تغییر رد می‌شود
        // تا هرگز تراکنش کسی Dispose نشود.
        EnsureNoAmbientTransaction();

        if (isNew)
        {
            return await CreateCapturedSelectionAsync(draft, expectedSelectionRevision, cancellationToken);
        }

        // گراف authoritative ذخیره‌شده (تازه و tracked)؛ هیچ‌گاه حقیقتِ شیء ورودی یا identity mapِ کهنه.
        var persisted = await db.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .FirstOrDefaultAsync(x => x.CartId == draft.CartId, cancellationToken)
            ?? throw Conflict();

        // CAS فقط با نسخهٔ <b>ذخیره‌شده</b>؛ مقدار تشخیصی شیء ورودی مرجع نیست.
        if (persisted.SelectionRevision != expectedSelectionRevision)
        {
            throw Conflict();
        }

        var root = db.Entry(persisted);

        // TB-232-R2: ورودی‌های cleanup پیش از <b>هر</b> تغییر گرفته می‌شوند تا detach دقیقاً همین‌ها را بگیرد و
        // به‌هیچ‌وجه به state جاری (که ممکن است پاک‌سازی خراب شود) تکیه نکند.
        var previousGroups = persisted.ShippingGroups.ToArray();
        var previousLines = previousGroups.SelectMany(group => group.Lines).ToArray();
        var attemptedGroups = groups.ToArray();
        var attemptedLines = attemptedGroups.SelectMany(group => group.Lines).ToArray();

        IDbContextTransaction? transaction = null;
        try
        {
            // شروع تراکنش هم داخل همان try است تا شکست آن نیز همین تلاش را پاک کند.
            transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            persisted.Replace(
                draft.GuestSecretHash,
                draft.CartVersion,
                draft.RecipientName,
                draft.ContactMobile,
                draft.ProvinceName,
                draft.CityName,
                draft.PostalAddress,
                draft.PostalCode,
                draft.SavedAddressId,
                draft.ShippingMethodCode,
                draft.ShippingMethodLabel,
                draft.ShippingAmount,
                draft.MinimumDeliveryDate,
                draft.SelectedDeliveryDate,
                draft.SelectedDeliveryTimeWindow,
                draft.CustomerNote,
                draft.UpdatedAt);
            persisted.ApplyRecipientNames(draft.FirstName, draft.LastName);
            persisted.SetDestinationIdentity(
                draft.DestinationCountryId ?? Guid.Empty,
                draft.DestinationAdministrativeAreaId,
                draft.DestinationCityId,
                draft.DestinationServiceAreaId,
                draft.UpdatedAt);
            persisted.BumpSelectionRevision(draft.UpdatedAt);
            root.Property(x => x.SelectionRevision).IsModified = true;

            // حذف‌ها و به‌روزرسانی ریشه در همان تراکنش؛ حذفِ durable جزئی هرگز اتفاق نمی‌افتد.
            persisted.ClearShippingGroups(draft.UpdatedAt);
            await db.SaveChangesAsync(cancellationToken);
            persisted.AddShippingGroups(groups, draft.UpdatedAt);
            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return persisted;
        }
        catch (Exception error)
        {
            // TB-232-R2: rollback/dispose/recovery خطای دوم را هرگز جای خطای اصلی نمی‌گذارد و detach حتی با
            // شکست rollback اجرا می‌شود. مالکیت تراکنش همین‌جا مصرف می‌شود (یک‌بار Dispose) و finally دیگر
            // چیزی برای Dispose ندارد.
            await RestoreRejectedAttemptAsync(
                transaction,
                persisted,
                previousGroups,
                previousLines,
                attemptedGroups,
                attemptedLines);
            transaction = null;

            if (IsStaleOrConflicting(error))
            {
                throw Conflict();
            }

            throw;
        }
        finally
        {
            await DisposeOwnedAsync(transaction);
        }
    }

    /// <summary>ساخت ریشه در یک تراکنش؛ شکست (حتی شروع تراکنش) گراف همین تلاش را پاک می‌کند.</summary>
    private async Task<CartShippingDraft> CreateCapturedSelectionAsync(
        CartShippingDraft draft,
        long expectedSelectionRevision,
        CancellationToken cancellationToken)
    {
        // نبود ریشه فقط با نسخهٔ ۰ (یعنی «هیچ انتخاب ثبت‌شده‌ای») قابل ساخت است.
        if (expectedSelectionRevision != 0)
        {
            throw Conflict();
        }

        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            draft.BumpSelectionRevision(draft.UpdatedAt);
            db.ShippingDrafts.Add(draft);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return draft;
        }
        catch (Exception error)
        {
            // TB-232-R2: rollback با توکن غیرلغوشده، سپس Dispose، و detach مستقل از نتیجهٔ rollback.
            await RollbackAndDisposeAsync(transaction);
            transaction = null;

            DetachDraftGraph(draft);
            if (IsStaleOrConflicting(error))
            {
                throw Conflict();
            }

            throw;
        }
        finally
        {
            await DisposeOwnedAsync(transaction);
        }
    }

    /// <summary>
    /// پس از شکست، <b>فقط همین تلاش</b> بازگردانده می‌شود. ترتیب عمدی است و هر مرحله خطای اصلی را حفظ
    /// می‌کند: rollback با توکن غیرلغوشده، سپس Dispose همان تراکنشِ خودمان (پیش از هر بازخوانی)، سپس detach
    /// گروه/خطوط همین تلاش و گراف قبلی همان ریشه (حتی اگر rollback شکست بخورد)، و در پایان بازخوانی گراف
    /// رول‌بک‌شده فقط وقتی rollback واقعاً موفق بوده است. اگر rollback ناموفق باشد هیچ گرافِ «بازسازی‌شده»ای
    /// ساخته نمی‌شود: گراف همین تلاش detached می‌ماند، هیچ نوشتن در انتظاری باقی نمی‌ماند و خطای اصلی دست‌نخورده
    /// می‌ماند (محدودیت در RESULT ثبت شده است). هیچ پاک‌سازی سراسری/نامرتبطی انجام نمی‌شود.
    /// </summary>
    /// <returns>آیا حقیقت رول‌بک‌شده از DB بازخوانی شد.</returns>
    private async Task<bool> RestoreRejectedAttemptAsync(
        IDbContextTransaction? transaction,
        CartShippingDraft persisted,
        IReadOnlyList<CartShippingDraftGroup> previousGroups,
        IReadOnlyList<CartShippingDraftGroupLine> previousLines,
        IReadOnlyList<CartShippingDraftGroup> attemptedGroups,
        IReadOnlyList<CartShippingDraftGroupLine> attemptedLines)
    {
        var rolledBack = await RollbackAndDisposeAsync(transaction);

        // برخلاف rollback، این بخش هرگز نمی‌تواند از کار بیفتد: ورودی‌ها پیش از تغییر گرفته شده‌اند.
        foreach (var attempted in attemptedGroups)
        {
            DetachGroup(attempted);
        }

        foreach (var line in attemptedLines)
        {
            DetachLine(line);
        }

        foreach (var previous in previousGroups)
        {
            DetachGroup(previous);
        }

        foreach (var line in previousLines)
        {
            DetachLine(line);
        }

        var rootEntry = db.Entry(persisted);
        if (rootEntry.State != EntityState.Detached)
        {
            rootEntry.State = EntityState.Detached;
        }

        if (!rolledBack)
        {
            // rollback تأیید نشد ⇒ هیچ بازخوانی‌ای انجام نمی‌شود تا گراف تأییدنشده به‌عنوان حقیقت جا نزند.
            return false;
        }

        // حقیقت رول‌بک‌شده دوباره از DB خوانده می‌شود؛ نه حالت دست‌سازِ ناوبری/state. تراکنش پیش‌تر
        // Dispose شده است و شکست همین بازخوانی هم نباید خطای اصلی را جایگزین کند.
        try
        {
            await db.ShippingDrafts
                .Include(x => x.ShippingGroups)
                .ThenInclude(x => x.Lines)
                .FirstOrDefaultAsync(x => x.CartId == persisted.CartId, CancellationToken.None);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// rollback با توکن غیرلغوشده و سپس Dispose همان تراکنشِ خودمان؛ هیچ خطای ثانویه‌ای جای خطای اصلی
    /// نمی‌نشیند. فقط «تراکنش پیش‌تر بسته شده» و موفقیت rollback «بازیابی‌شده» شمرده می‌شوند.
    /// </summary>
    private static async Task<bool> RollbackAndDisposeAsync(IDbContextTransaction? transaction)
    {
        if (transaction is null)
        {
            return false;
        }

        var rolledBack = false;
        try
        {
            await transaction.RollbackAsync(CancellationToken.None);
            rolledBack = true;
        }
        catch (InvalidOperationException)
        {
            // تراکنش پیش‌تر بسته شده است؛ وضعیت DB نامعلوم است، پس «بازیابی‌شده» ادعا نمی‌شود.
        }
        catch (Exception)
        {
            // شکست واقعی rollback (provider/شبکه): خطای اصلی حفظ می‌شود و بازیابی ادعا نمی‌شود.
        }
        finally
        {
            await DisposeOwnedAsync(transaction);
        }

        return rolledBack;
    }

    /// <summary>Dispose یک‌بارهٔ تراکنشِ خودمان؛ شکست Dispose هرگز نتیجهٔ اصلی را عوض نمی‌کند.</summary>
    private static async Task DisposeOwnedAsync(IDbContextTransaction? transaction)
    {
        if (transaction is null)
        {
            return;
        }

        try
        {
            await transaction.DisposeAsync();
        }
        catch (Exception)
        {
            // Dispose ثانویه است: نه موفقیت را باطل می‌کند و نه خطای اصلی را جایگزین می‌شود.
        }
    }

    /// <summary>
    /// پیش‌شرط نوشتن: هر کار Added/Modified/Deleted در همین context «کار در انتظار» است و نوشتن
    /// capture را می‌بندد. هیچ ورودی‌ای تغییر نمی‌کند؛ فقط رد می‌شود.
    /// </summary>
    private void EnsureNoForeignPendingWork()
    {
        foreach (var entry in db.ChangeTracker.Entries())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                throw new StorefrontOrderException(StorefrontCapturedSelectionReasons.PendingWork);
            }
        }
    }

    /// <summary>
    /// تراکنش بازِ فراخوان مالکیت داراست: این آداپتور نه آن را adopt می‌کند و نه تودرتو باز می‌کند. پیش از هر
    /// تغییر رد می‌شود تا هیچ‌گاه تراکنشی که ما مالکش نیستیم Rollback/Dispose نشود.
    /// </summary>
    private void EnsureNoAmbientTransaction()
    {
        if (db.Database.CurrentTransaction is not null)
        {
            // همان معنای «کار ناتمامِ فراخوان»: نوشتن capture تا پایان آن واحدِ کار ممکن نیست.
            throw new StorefrontOrderException(StorefrontCapturedSelectionReasons.PendingWork);
        }
    }

    private void DetachDraftGraph(CartShippingDraft draft)
    {
        foreach (var group in draft.ShippingGroups.ToArray())
        {
            DetachGroup(group);
        }

        var entry = db.Entry(draft);
        if (entry.State != EntityState.Detached)
        {
            entry.State = EntityState.Detached;
        }
    }

    private static bool IsStaleOrConflicting(Exception error) =>
        error is DbUpdateConcurrencyException
        || (error is DbUpdateException update && IsRootUniquenessViolation(update));

    private void DetachGroup(CartShippingDraftGroup group)
    {
        var entry = db.Entry(group);
        if (entry.State != EntityState.Detached)
        {
            entry.State = EntityState.Detached;
        }

        foreach (var line in group.Lines.ToArray())
        {
            DetachLine(line);
        }
    }

    private void DetachLine(CartShippingDraftGroupLine line)
    {
        var lineEntry = db.Entry(line);
        if (lineEntry.State != EntityState.Detached)
        {
            lineEntry.State = EntityState.Detached;
        }
    }

    private static bool IsRootUniquenessViolation(DbUpdateException error)
    {
        for (var current = error as Exception; current is not null; current = current.InnerException)
        {
            if (current is Npgsql.PostgresException postgres
                && postgres.SqlState == "23505"
                && string.Equals(postgres.ConstraintName, "pk_cart_shipping_drafts", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static StorefrontOrderException Conflict() =>
        new(StorefrontShippingSelectionConflict.Stale);
}

public sealed class StorefrontPendingCheckoutStore(OrderDbContext db) : IStorefrontPendingCheckoutStore
{
    public async Task<IReadOnlyList<CheckoutGroup>> ListOwnedByUserAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken) =>
        await db.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .Where(x => x.PlacedByUserId == userId)
            .OrderByDescending(x => x.SubmittedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CheckoutGroup>> ListByCheckoutIdsAsync(
        IReadOnlyList<Guid> checkoutIds,
        CancellationToken cancellationToken) =>
        await db.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .ThenInclude(x => x.Lines)
            .Where(x => checkoutIds.Contains(x.CheckoutId))
            .ToListAsync(cancellationToken);

    public Task<CheckoutGroup?> GetWithSellerOrdersAsync(Guid checkoutId, CancellationToken cancellationToken) =>
        db.Checkouts.AsNoTracking()
            .Include(x => x.SellerOrders)
            .SingleOrDefaultAsync(x => x.CheckoutId == checkoutId, cancellationToken);

    public async Task HidePendingCardAsync(
        Guid checkoutId,
        Guid? ownerUserId,
        Guid? guestCartId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var exists = await db.PendingPaymentCardHides.AsNoTracking()
            .AnyAsync(
                x => x.CheckoutId == checkoutId
                    && (ownerUserId != null
                        ? x.OwnerUserId == ownerUserId
                        : x.GuestCartId == guestCartId),
                cancellationToken);
        if (!exists)
        {
            db.PendingPaymentCardHides.Add(
                PendingPaymentCardHide.Create(checkoutId, ownerUserId, guestCartId, now));
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<HashSet<Guid>> LoadHiddenCheckoutIdsAsync(
        IReadOnlyList<Guid> checkoutIds,
        Guid? ownerUserId,
        IReadOnlyList<Guid> guestCartIds,
        CancellationToken cancellationToken)
    {
        if (checkoutIds.Count == 0)
        {
            return [];
        }

        if (ownerUserId is Guid userId)
        {
            var rows = await db.PendingPaymentCardHides.AsNoTracking()
                .Where(x => x.OwnerUserId == userId && checkoutIds.Contains(x.CheckoutId))
                .Select(x => x.CheckoutId)
                .ToListAsync(cancellationToken);
            return [.. rows];
        }

        var guestRows = await db.PendingPaymentCardHides.AsNoTracking()
            .Where(x =>
                x.GuestCartId != null
                && guestCartIds.Contains(x.GuestCartId.Value)
                && checkoutIds.Contains(x.CheckoutId))
            .Select(x => x.CheckoutId)
            .ToListAsync(cancellationToken);
        return [.. guestRows];
    }
}
