#pragma warning disable CS1591
using Microsoft.EntityFrameworkCore;
using Tooba.Order.Application.Storefront;
using Tooba.Order.Application.Storefront.Ports;
using Tooba.Order.Domain;
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
    public async Task ReplaceCapturedSelectionAsync(
        CartShippingDraft draft,
        IReadOnlyList<CartShippingDraftGroup> groups,
        bool isNew,
        long expectedSelectionRevision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(groups);

        var previousGroups = draft.ShippingGroups.ToArray();
        var previousRevision = draft.SelectionRevision;
        var now = draft.UpdatedAt;

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (isNew)
            {
                // نبود ریشه فقط با نسخهٔ ۰ (یعنی «هیچ انتخاب ثبت‌شده‌ای») قابل ساخت است.
                if (expectedSelectionRevision != 0)
                {
                    throw Conflict();
                }

                draft.BumpSelectionRevision(now);
                db.ShippingDrafts.Add(draft);
                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                // CAS صریح روی نسخهٔ مشاهده‌شده؛ توکن هم‌زمانی EF همین ستون را در UPDATE هم می‌آورد.
                if (draft.SelectionRevision != expectedSelectionRevision)
                {
                    throw Conflict();
                }

                draft.BumpSelectionRevision(now);
                var entry = db.Entry(draft);
                entry.Property(x => x.SelectionRevision).IsModified = true;

                // حذف‌ها و به‌روزرسانی ریشه در همان تراکنش؛ حذفِ durable جزئی هرگز اتفاق نمی‌افتد.
                draft.ClearShippingGroups(now);
                await db.SaveChangesAsync(cancellationToken);
                draft.AddShippingGroups(groups, now);
                await db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackOwnChangesAsync(draft, previousGroups, groups, previousRevision, isNew, cancellationToken);
            throw Conflict();
        }
        catch (DbUpdateException error) when (IsRootUniquenessViolation(error))
        {
            await RollbackOwnChangesAsync(draft, previousGroups, groups, previousRevision, isNew, cancellationToken);
            throw Conflict();
        }
        catch
        {
            await RollbackOwnChangesAsync(draft, previousGroups, groups, previousRevision, isNew, cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// تراکنش را برمی‌گرداند و <b>فقط</b> تغییرات همین تلاش را از tracking پاک می‌کند: گروه‌های
    /// درج‌نشده detach می‌شوند، گروه‌های حذف‌شده به Unchanged برمی‌گردند و اسکالرهای ریشه به مقادیر
    /// اصلی خود بازمی‌گردند. هیچ موجودیت نامرتبطی flush/clear نمی‌شود.
    /// </summary>
    private async Task RollbackOwnChangesAsync(
        CartShippingDraft draft,
        IReadOnlyList<CartShippingDraftGroup> previousGroups,
        IReadOnlyList<CartShippingDraftGroup> attemptedGroups,
        long previousRevision,
        bool isNew,
        CancellationToken cancellationToken)
    {
        try
        {
            await db.Database.RollbackTransactionAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            // تراکنش پیش‌تر بسته شده است؛ rollback دوباره لازم نیست.
        }

        // هر گروهی که همین تلاش درج کرده است — حتی اگر پس از شکست از مجموعهٔ ریشه بیرون افتاده باشد —
        // صریحاً detach می‌شود تا SaveChanges بعدی هرگز آن را جاودانه نکند.
        foreach (var attempted in attemptedGroups)
        {
            DetachGroup(attempted);
        }

        var root = db.Entry(draft);
        if (isNew)
        {
            root.State = EntityState.Detached;
            foreach (var group in draft.ShippingGroups.ToArray())
            {
                DetachGroup(group);
            }

            return;
        }

        root.CurrentValues.SetValues(root.OriginalValues);
        root.State = EntityState.Unchanged;

        // نسخهٔ انتخاب نیز به مقدار مشاهده‌شدهٔ پیش از همین تلاش برمی‌گردد: SaveChanges مرحلهٔ اول
        // OriginalValues را به مقدار bumpشده به‌روز کرده است، پس بازگردانی از OriginalValues کافی نیست.
        draft.RestoreSelectionRevision(previousRevision);
        root.Property(x => x.SelectionRevision).OriginalValue = previousRevision;
        root.CurrentValues[nameof(CartShippingDraft.SelectionRevision)] = previousRevision;
        root.Property(x => x.SelectionRevision).IsModified = false;

        if (previousGroups.Count > 0)
        {
            foreach (var group in draft.ShippingGroups.ToArray())
            {
                if (previousGroups.Contains(group))
                {
                    continue;
                }

                DetachGroup(group);
            }

            draft.ClearShippingGroups(draft.UpdatedAt);
            draft.AddShippingGroups(previousGroups, draft.UpdatedAt);
            foreach (var group in previousGroups)
            {
                var entry = db.Entry(group);
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Unchanged;
                }
            }
        }

        // آخرین پاک‌سازی قطعی: در پایان، state هر گروهِ این پیش‌نویس صریحاً تثبیت می‌شود — گروه‌های
        // قبلی Unchanged و هر چیز دیگری (درج‌شده یا حذف‌شدهٔ همین تلاش) detach. پس SaveChanges بعدی
        // نمی‌تواند انتخاب ردشده را جاودانه کند و گراف قبلی هم دست‌نخورده می‌ماند.
        var keep = previousGroups.Select(group => group.ShippingGroupId).ToHashSet();
        foreach (var entry in db.ChangeTracker.Entries<CartShippingDraftGroup>().ToArray())
        {
            if (entry.Entity.CartId != draft.CartId)
            {
                continue;
            }

            if (keep.Contains(entry.Entity.ShippingGroupId))
            {
                if (entry.State != EntityState.Unchanged)
                {
                    entry.State = EntityState.Unchanged;
                }

                continue;
            }

            DetachGroup(entry.Entity);
        }

        // خطوط گروه‌های حذف‌شده هم صریحاً پاک می‌شوند: با detach شدن گروه، ناوبری خطوطش خالی می‌شود و
        // اگر این‌جا رها شوند SaveChanges بعدی آن‌ها را دوباره درج می‌کرد.
        foreach (var lineEntry in db.ChangeTracker.Entries<CartShippingDraftGroupLine>().ToArray())
        {
            if (keep.Contains(lineEntry.Entity.ShippingGroupId))
            {
                if (lineEntry.State != EntityState.Unchanged)
                {
                    lineEntry.State = EntityState.Unchanged;
                }

                continue;
            }

            if (lineEntry.State != EntityState.Detached)
            {
                lineEntry.State = EntityState.Detached;
            }
        }

        // AddShippingGroups مشتق‌های اسکالر را بازنویسی می‌کند؛ مقدار اصلی دوباره حاکم می‌شود.
        root.CurrentValues.SetValues(root.OriginalValues);
        root.State = EntityState.Unchanged;
    }
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
