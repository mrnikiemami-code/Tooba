using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Shipping.Models;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Contracts.Shipping;
using Tooba.Fulfillment.Domain.Aggregates;
using Tooba.Fulfillment.Infrastructure.Persistence;

namespace Tooba.Fulfillment.Infrastructure.Shipping;

/// <summary>
/// پایدارسازی/خواندن کاتالوگ نوع ارسال عملیاتی روی <see cref="FulfillmentDbContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// تنها نویسندهٔ این جدول‌ها همین درز است. هر نوشتن یک <c>SaveChanges</c> است و هیچ تراکنش صریح یا نوشتن
/// بین‌ماژولی ساخته نمی‌شود.
/// </para>
/// <para>
/// قاعدهٔ اتمیک: کل وضعیت پیشنهادی (سرصفحه + همهٔ ترجمه‌ها) پیش از نخستین تغییر tracked آماده و
/// اعتبارسنجی می‌شود و اختلاف معنایی واقعی محاسبه می‌شود. اگر هیچ اختلافی نباشد، هیچ موجودیتی تغییر نمی‌کند؛
/// پس بازبینی و UpdatedAt دست‌نخورده می‌مانند. یک ورودی نامعتبر — حتی آخرین عضو فهرست — هیچ تغییر معلقی
/// باقی نمی‌گذارد و در SaveChanges بعدی منتشر نمی‌شود.
/// </para>
/// <para>
/// یکتایی کد/جفت زبان از SQLSTATE <c>23505</c> روی همان نام قید مالک‌شده (برابری کامل) به خطای تجاری پایدار
/// نگاشت می‌شود، نه از متن پیام provider و نه از تطبیق زیررشته. هر خطای دیگر دوباره پرتاب می‌شود.
/// </para>
/// </remarks>
public sealed class ShippingTypeStore : IShippingTypeStore, IShippingTypeReader
{
    private const string CodeIndexName = "ix_shipping_types_code";
    private const string TranslationIndexName = "ix_shipping_type_translations_type_language";

    private readonly FulfillmentDbContext _db;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>سازنده.</summary>
    public ShippingTypeStore(FulfillmentDbContext db, IClock clock, IIdGenerator ids)
    {
        _db = db;
        _clock = clock;
        _ids = ids;
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(
        string code,
        int sortOrder,
        IReadOnlyList<ShippingTypePreparedTranslation> translations,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(translations);

        var normalizedCode = ShippingType.TryNormalizeCode(code)
                             ?? throw Fault(FulfillmentErrorCodes.ShippingTypeCodeInvalid);

        // همهٔ ترجمه‌ها پیش از هر تغییر tracked دوباره با قاعدهٔ واحد canonical بررسی می‌شوند.
        var prepared = PrepareAll(translations);

        if (await _db.ShippingTypes.AnyAsync(x => x.Code == normalizedCode, cancellationToken))
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeCodeDuplicate);
        }

        try
        {
            var id = _ids.NewId();
            var now = _clock.UtcNow;
            var entity = ShippingType.Create(id, normalizedCode, sortOrder, [], now);

            // تعریف تازه همیشه غیرفعال است؛ فعال‌سازی فقط از مسیر ویرایش با نام پیش‌فرض موجود می‌آید.
            foreach (var translation in prepared)
            {
                entity.AddTranslation(_ids.NewId(), translation.LanguageId, translation.Name, translation.Description);
            }

            _db.ShippingTypes.Add(entity);
            await _db.SaveChangesAsync(cancellationToken);
            return id;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, CodeIndexName))
        {
            _db.ChangeTracker.Clear();
            throw Fault(FulfillmentErrorCodes.ShippingTypeCodeDuplicate);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, TranslationIndexName))
        {
            _db.ChangeTracker.Clear();
            throw Fault(FulfillmentErrorCodes.ShippingTypeTranslationInvalid);
        }
        catch
        {
            _db.ChangeTracker.Clear();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ShippingTypeView> UpdateAsync(
        Guid shippingTypeId,
        long expectedRevision,
        string? submittedCode,
        int sortOrder,
        bool isActive,
        IReadOnlyList<ShippingTypePreparedTranslation> translations,
        Guid activeDefaultLanguageId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(translations);

        if (shippingTypeId == Guid.Empty)
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeNotFound);
        }

        var entity = await _db.ShippingTypes
                         .Include(x => x.Translations)
                         .FirstOrDefaultAsync(x => x.ShippingTypeId == shippingTypeId, cancellationToken)
                     ?? throw Fault(FulfillmentErrorCodes.ShippingTypeNotFound);

        // کد هویت تغییرناپذیر است: بدنهٔ حاوی کد متفاوت رد می‌شود، نه اینکه بی‌صدا نادیده گرفته شود.
        if (!string.IsNullOrEmpty(submittedCode))
        {
            var submitted = ShippingType.TryNormalizeCode(submittedCode)
                            ?? throw Fault(FulfillmentErrorCodes.ShippingTypeCodeInvalid);
            if (!string.Equals(submitted, entity.Code, StringComparison.Ordinal))
            {
                throw Fault(FulfillmentErrorCodes.ShippingTypeCodeInvalid);
            }
        }

        // کهنه‌بودن بازبینی پیش از no-op بررسی می‌شود: بدنهٔ یکسان با بازبینی کهنه هم تعارض است.
        if (entity.Revision != expectedRevision)
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeRevisionConflict);
        }

        // 1) آماده‌سازی/اعتبارسنجی کامل patch و وضعیت پیشنهادی، بدون هیچ تغییر tracked.
        var prepared = PrepareAll(translations);
        var proposed = BuildProposed(entity, prepared);

        // 2) فعال‌سازی نیازمند نام برای پیش‌فرض فعال در وضعیت پیشنهادی است.
        if (isActive && !proposed.ContainsKey(activeDefaultLanguageId))
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeDefaultLanguageNameRequired);
        }

        // 3) اختلاف معنایی واقعی روی مقادیر canonical — نه فقط «patch خالی نیست».
        var changed = entity.SortOrder != sortOrder
                      || entity.IsActive != isActive
                      || prepared.Any(patch => Differs(entity, patch));

        if (!changed)
        {
            return ToView(entity, activeDefaultLanguageId);
        }

        // 4) اعمال اتمیک پس از تأیید کامل.
        entity.Update(sortOrder, isActive, _clock.UtcNow);
        foreach (var patch in prepared)
        {
            if (entity.HasTranslation(patch.LanguageId))
            {
                entity.ReplaceTranslation(patch.LanguageId, patch.Name, patch.Description);
            }
            else
            {
                entity.AddTranslation(_ids.NewId(), patch.LanguageId, patch.Name, patch.Description);
            }
        }

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            _db.ChangeTracker.Clear();
            throw Fault(FulfillmentErrorCodes.ShippingTypeRevisionConflict);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, TranslationIndexName))
        {
            _db.ChangeTracker.Clear();
            throw Fault(FulfillmentErrorCodes.ShippingTypeTranslationInvalid);
        }
        catch
        {
            _db.ChangeTracker.Clear();
            throw;
        }

        return ToView(entity, activeDefaultLanguageId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingTypeSummaryView>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.ShippingTypes
            .AsNoTracking()
            .Include(x => x.Translations)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ShippingTypeId)
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new ShippingTypeSummaryView(
                x.ShippingTypeId,
                x.Code,
                x.IsActive,
                x.SortOrder,
                x.Revision,
                ToTranslations(x)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<ShippingTypeView?> GetAsync(
        Guid shippingTypeId,
        Guid? activeDefaultLanguageId,
        CancellationToken cancellationToken)
    {
        if (shippingTypeId == Guid.Empty)
        {
            return null;
        }

        var entity = await _db.ShippingTypes
            .AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.ShippingTypeId == shippingTypeId, cancellationToken);

        return entity is null ? null : ToView(entity, activeDefaultLanguageId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingTypeSnapshot>> ListActiveAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.ShippingTypes
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ShippingTypeId)
            .ToListAsync(cancellationToken);

        return rows.Select(ToSnapshot).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ShippingTypeSnapshot>> GetByIdsAsync(
        IReadOnlyCollection<Guid> shippingTypeIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shippingTypeIds);
        if (shippingTypeIds.Count == 0)
        {
            return [];
        }

        var ids = shippingTypeIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        var rows = await _db.ShippingTypes
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => ids.Contains(x.ShippingTypeId))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.ShippingTypeId)
            .ToListAsync(cancellationToken);

        return rows.Select(ToSnapshot).ToList();
    }

    private static IReadOnlyList<ShippingTypePreparedTranslation> PrepareAll(
        IReadOnlyList<ShippingTypePreparedTranslation> translations)
    {
        var prepared = new List<ShippingTypePreparedTranslation>(translations.Count);
        var seen = new HashSet<Guid>();

        foreach (var translation in translations)
        {
            if (translation is null || !seen.Add(translation.LanguageId))
            {
                throw Fault(FulfillmentErrorCodes.ShippingTypeLanguageInvalid);
            }

            var (name, description) = ShippingTypePreparation.NormalizeText(
                translation.LanguageId,
                translation.Name,
                translation.Description);

            prepared.Add(new ShippingTypePreparedTranslation(translation.LanguageId, name, description));
        }

        return prepared;
    }

    private static Dictionary<Guid, string> BuildProposed(
        ShippingType entity,
        IReadOnlyList<ShippingTypePreparedTranslation> patches)
    {
        var proposed = entity.Translations
            .ToDictionary(x => x.LanguageId, x => x.Name);

        foreach (var patch in patches)
        {
            proposed[patch.LanguageId] = patch.Name;
        }

        return proposed;
    }

    private static bool Differs(ShippingType entity, ShippingTypePreparedTranslation patch)
    {
        var stored = entity.Translations.FirstOrDefault(x => x.LanguageId == patch.LanguageId);
        if (stored is null)
        {
            return true;
        }

        return !string.Equals(stored.Name, patch.Name, StringComparison.Ordinal)
               || !string.Equals(stored.Description, patch.Description, StringComparison.Ordinal);
    }

    private static ShippingTypeView ToView(ShippingType entity, Guid? defaultLanguageId)
    {
        var translations = ToTranslations(entity);
        return new ShippingTypeView(
            entity.ShippingTypeId,
            entity.Code,
            entity.IsActive,
            entity.SortOrder,
            entity.Revision,
            translations,
            defaultLanguageId,
            defaultLanguageId is { } id
            && translations.Any(x => x.LanguageId == id && !string.IsNullOrWhiteSpace(x.Name)));
    }

    private static ShippingTypeSnapshot ToSnapshot(ShippingType entity) =>
        new(entity.ShippingTypeId, entity.Code, entity.IsActive, entity.SortOrder, entity.Revision,
            ToTranslations(entity));

    private static IReadOnlyList<ShippingTypeTranslationSnapshot> ToTranslations(ShippingType entity) =>
        entity.Translations
            .OrderBy(x => x.LanguageId)
            .Select(x => new ShippingTypeTranslationSnapshot(x.LanguageId, x.Name, x.Description))
            .ToList();

    private static SemanticException Fault(string code) => new(new SemanticError(code));

    /// <summary>
    /// فقط یکتایی PostgreSQL روی همان نام قید مالک‌شده (برابری کامل ordinal) «تکراری» است؛ قید
    /// lookalike یا هر <see cref="DbUpdateException"/> دیگر دوباره پرتاب می‌شود.
    /// </summary>
    private static bool IsUniqueViolation(DbUpdateException exception, string constraintName)
        => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
           && string.Equals(postgres.ConstraintName, constraintName, StringComparison.Ordinal);
}
