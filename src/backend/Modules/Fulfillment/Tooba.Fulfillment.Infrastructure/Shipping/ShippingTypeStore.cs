using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.ShippingTypes;
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
/// بین‌ماژولی ساخته نمی‌شود. پیش‌بررسی کامل (شکل کد/ترتیب/متن، زبان فعال، یکتایی کد و نام پیش‌فرض برای
/// فعال‌سازی) پیش از تغییر وضعیت tracked انجام می‌شود؛ روی رد شدن، ردیابی پاک می‌شود.
/// </para>
/// <para>
/// یکتایی کد و جفت زبان از SQLSTATE <c>23505</c> روی همان قید نام‌برده به خطای تجاری پایدار نگاشت می‌شود،
/// نه از متن پیام provider. هر خطای دیگر دوباره پرتاب می‌شود.
/// </para>
/// </remarks>
public sealed class ShippingTypeStore : IShippingTypeStore, IShippingTypeReader
{
    private const string CodeConstraint = "ix_shipping_types_code";
    private const string TranslationConstraint = "ix_shipping_type_translations_shipping_type_id_language_id";

    private readonly FulfillmentDbContext _db;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;
    private readonly IShippingTypeLanguageFacts _languageFacts;

    /// <summary>سازنده.</summary>
    public ShippingTypeStore(
        FulfillmentDbContext db,
        IClock clock,
        IIdGenerator ids,
        IShippingTypeLanguageFacts languageFacts)
    {
        _db = db;
        _clock = clock;
        _ids = ids;
        _languageFacts = languageFacts;
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(ShippingTypeCreateModel model, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        var code = ShippingType.TryNormalizeCode(model.Code)
                   ?? throw Fault(FulfillmentErrorCodes.ShippingTypeCodeInvalid);

        if (await _db.ShippingTypes.AnyAsync(x => x.Code == code, cancellationToken))
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeCodeDuplicate);
        }

        var id = _ids.NewId();
        var now = _clock.UtcNow;
        var entity = ShippingType.Create(id, code, model.SortOrder, [], now);

        // تعریف تازه همیشه غیرفعال است؛ فعال‌سازی فقط از مسیر ویرایش با نام پیش‌فرض موجود می‌آید.
        foreach (var translation in model.Translations)
        {
            entity.AddTranslation(translation.LanguageId, translation.Name, translation.Description);
        }

        _db.ShippingTypes.Add(entity);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, CodeConstraint))
        {
            ClearTracking();
            throw Fault(FulfillmentErrorCodes.ShippingTypeCodeDuplicate);
        }
        catch (DbUpdateException expectation) when (IsUniqueViolation(expectation, TranslationConstraint))
        {
            ClearTracking();
            throw Fault(FulfillmentErrorCodes.ShippingTypeTranslationInvalid);
        }
        catch
        {
            ClearTracking();
            throw;
        }

        return id;
    }

    /// <inheritdoc />
    public async Task<ShippingTypeView> UpdateAsync(
        Guid shippingTypeId,
        ShippingTypeUpdateModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (shippingTypeId == Guid.Empty)
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeNotFound);
        }

        var entity = await _db.ShippingTypes
                         .Include(x => x.Translations)
                         .FirstOrDefaultAsync(x => x.ShippingTypeId == shippingTypeId, cancellationToken)
                     ?? throw Fault(FulfillmentErrorCodes.ShippingTypeNotFound);

        // کد هویت تغییرناپذیر است: بدنهٔ حاوی کد متفاوت رد می‌شود، نه اینکه بی‌صدا نادیده گرفته شود.
        if (!string.IsNullOrWhiteSpace(model.Code))
        {
            var submitted = ShippingType.TryNormalizeCode(model.Code)
                            ?? throw Fault(FulfillmentErrorCodes.ShippingTypeCodeInvalid);
            if (!string.Equals(submitted, entity.Code, StringComparison.Ordinal))
            {
                throw Fault(FulfillmentErrorCodes.ShippingTypeCodeInvalid);
            }
        }

        // کهنه‌بودن بازبینی پیش از هر no-op بررسی می‌شود: بدنهٔ یکسان هم با بازبینی کهنه تعارض است.
        if (entity.Revision != model.ExpectedRevision)
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeRevisionConflict);
        }

        var facts = await _languageFacts.ReadAsync(cancellationToken);
        var defaultLanguage = facts.ActiveDefault;

        // فعال‌سازی/فعال‌نگه‌داشتن نیازمند نام ذخیره‌شده یا همین patch برای پیش‌فرض فعال است.
        if (model.IsActive)
        {
            if (defaultLanguage is null)
            {
                throw Fault(FulfillmentErrorCodes.ShippingTypeDefaultLanguageUnavailable);
            }

            var patched = model.Translations.FirstOrDefault(x => x.LanguageId == defaultLanguage.LanguageId);
            var nameAfter = patched?.Name ?? (entity.HasTranslation(defaultLanguage.LanguageId)
                ? entity.Translations.First(x => x.LanguageId == defaultLanguage.LanguageId).Name
                : null);

            if (string.IsNullOrWhiteSpace(nameAfter))
            {
                throw Fault(FulfillmentErrorCodes.ShippingTypeDefaultLanguageNameRequired);
            }
        }

        // patch فقط زبان‌های صریحاً فهرست‌شده را عوض می‌کند؛ [] هیچ متنی را حذف نمی‌کند.
        foreach (var translation in model.Translations)
        {
            if (!entity.HasTranslation(translation.LanguageId))
            {
                entity.AddTranslation(translation.LanguageId, translation.Name, translation.Description);
                continue;
            }

            entity.ReplaceTranslation(translation.LanguageId, translation.Name, translation.Description);
        }

        // بدنهٔ بدون تغییر: بازبینی و UpdatedAt باید دست‌نخورده بمانند (no-op صادق).
        var changed = entity.SortOrder != model.SortOrder
                      || entity.IsActive != model.IsActive
                      || model.Translations.Count > 0;
        if (!changed)
        {
            return ToView(entity, defaultLanguage?.LanguageId);
        }

        entity.Update(model.SortOrder, model.IsActive, _clock.UtcNow);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            ClearTracking();
            throw Fault(FulfillmentErrorCodes.ShippingTypeRevisionConflict);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, TranslationConstraint))
        {
            ClearTracking();
            throw Fault(FulfillmentErrorCodes.ShippingTypeTranslationInvalid);
        }
        catch
        {
            ClearTracking();
            throw;
        }

        return ToView(entity, defaultLanguage?.LanguageId);
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
    public async Task<ShippingTypeView?> GetAsync(Guid shippingTypeId, CancellationToken cancellationToken)
    {
        if (shippingTypeId == Guid.Empty)
        {
            return null;
        }

        var entity = await _db.ShippingTypes
            .AsNoTracking()
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.ShippingTypeId == shippingTypeId, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        var facts = await _languageFacts.ReadAsync(cancellationToken);
        return ToView(entity, facts.ActiveDefault?.LanguageId);
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

    private void ClearTracking() => _db.ChangeTracker.Clear();

    private static SemanticException Fault(string code) => new(new SemanticError(code));

    /// <summary>
    /// فقط یکتایی PostgreSQL روی همان قید نام‌برده «تکراری» است؛ هر <see cref="DbUpdateException"/> دیگر
    /// دوباره پرتاب می‌شود تا خطای provider هرگز به پاسخ تجاری تبدیل نشود.
    /// </summary>
    private static bool IsUniqueViolation(DbUpdateException exception, string constraintFragment)
        => exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres
           && postgres.ConstraintName?.Contains(constraintFragment, StringComparison.Ordinal) == true;
}
