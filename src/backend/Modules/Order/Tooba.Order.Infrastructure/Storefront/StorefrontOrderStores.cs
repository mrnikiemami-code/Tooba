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

        var previousGroups = persisted.ShippingGroups.ToArray();

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
            await RestoreRejectedAttemptAsync(persisted, previousGroups, groups, transaction);
            if (IsStaleOrConflicting(error))
            {
                throw Conflict();
            }

            throw;
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
            if (transaction is not null)
            {
                try
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                }
                catch (InvalidOperationException)
                {
                    // تراکنش پیش‌تر بسته شده است.
                }
            }

            DetachDraftGraph(draft);
            if (IsStaleOrConflicting(error))
            {
                throw Conflict();
            }

            throw;
        }
    }

    /// <summary>
    /// پس از شکست، <b>فقط همین تلاش</b> بازگردانده می‌شود: تراکنش با توکن غیرلغوشده برمی‌گردد، گراف همین
    /// تلاش (گروه‌های درج‌نشده و گروه/خطوط قبلی) و ریشهٔ همین پیش‌نویس از tracking خارج می‌شوند و سپس ریشه
    /// دقیقاً از DB رول‌بک‌شده بازخوانی می‌شود. بنابراین هم CurrentValues و هم OriginalValues با مقدار پیش از
    /// تلاش برابرند، هیچ کار در انتظاری باقی نمی‌ماند و SaveChanges بعدی نمی‌تواند انتخاب ردشده را جاودانه
    /// کند. هیچ پاک‌سازی سراسری/نامرتبطی انجام نمی‌شود.
    /// </summary>
    private async Task RestoreRejectedAttemptAsync(
        CartShippingDraft persisted,
        IReadOnlyList<CartShippingDraftGroup> previousGroups,
        IReadOnlyList<CartShippingDraftGroup> attemptedGroups,
        IDbContextTransaction? transaction)
    {
        if (transaction is not null)
        {
            try
            {
                // لغو فراخوان نباید پاک‌سازی را نیمه‌کاره رها کند و نباید خطای اصلی را بپوشاند.
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch (InvalidOperationException)
            {
                // تراکنش پیش‌تر بسته/قطع شده است.
            }
        }

        // فقط گروه‌های همین تلاش و گروه‌های قبلیِ همان ریشه detach می‌شوند؛ هیچ sweep سراسری‌ای نیست.
        foreach (var attempted in attemptedGroups)
        {
            DetachGroup(attempted);
        }

        foreach (var previous in previousGroups)
        {
            DetachGroup(previous);
        }

        var rootEntry = db.Entry(persisted);
        if (rootEntry.State != EntityState.Detached)
        {
            rootEntry.State = EntityState.Detached;
        }

        // حقیقت رول‌بک‌شده دوباره از DB خوانده می‌شود؛ نه حالت دست‌سازِ ناوبری/state.
        await db.ShippingDrafts
            .Include(x => x.ShippingGroups)
            .ThenInclude(x => x.Lines)
            .FirstOrDefaultAsync(x => x.CartId == persisted.CartId, CancellationToken.None);
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
            var lineEntry = db.Entry(line);
            if (lineEntry.State != EntityState.Detached)
            {
                lineEntry.State = EntityState.Detached;
            }
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
