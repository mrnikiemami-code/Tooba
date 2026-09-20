using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Fulfillment.Application;
using Tooba.Fulfillment.Domain;
using Tooba.Fulfillment.Infrastructure.Persistence;

namespace Tooba.Fulfillment.Infrastructure;

/// <summary>orchestration نوشتن ShippingService روی Fulfillment DbContext.</summary>
public sealed class ShippingServiceDirectory : IShippingServiceDirectory
{
    private readonly FulfillmentDbContext _db;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;
    private readonly IShippingServiceLanguageGate _languages;

    /// <summary>دایرکتوری را به schema fulfillment وصل می‌کند.</summary>
    public ShippingServiceDirectory(
        FulfillmentDbContext db,
        IClock clock,
        IIdGenerator ids,
        IShippingServiceLanguageGate languages)
    {
        _db = db;
        _clock = clock;
        _ids = ids;
        _languages = languages;
    }

    /// <inheritdoc />
    public async Task<Guid> CreateAsync(ShippingServiceWriteModel model, CancellationToken cancellationToken)
    {
        await ValidateLanguagesAsync(model, cancellationToken);
        var code = model.Code.Trim().ToLowerInvariant();
        if (await _db.ShippingServices.AnyAsync(x => x.Code == code, cancellationToken))
        {
            throw new PlatformHttpException(409, "duplicate", "shipping_service.code_duplicate");
        }

        var now = _clock.UtcNow;
        var id = _ids.NewId();
        var entity = ShippingService.Create(
            id,
            model.Code,
            model.ProviderKind,
            model.IconKey,
            model.ColorKey,
            model.SortOrder,
            model.IsActive,
            now);
        _db.ShippingServices.Add(entity);
        AddTranslations(id, model.Translations);
        AddOptions(id, model.Options, now);
        await _db.SaveChangesAsync(cancellationToken);
        return id;
    }

    /// <inheritdoc />
    public async Task<Guid> UpdateAsync(Guid serviceId, ShippingServiceWriteModel model, CancellationToken cancellationToken)
    {
        await ValidateLanguagesAsync(model, cancellationToken);
        var entity = await _db.ShippingServices.FirstOrDefaultAsync(x => x.ShippingServiceId == serviceId, cancellationToken)
            ?? throw new PlatformHttpException(404, "not found", "shipping_service.not_found");

        var code = model.Code.Trim().ToLowerInvariant();
        if (await _db.ShippingServices.AnyAsync(x => x.Code == code && x.ShippingServiceId != serviceId, cancellationToken))
        {
            throw new PlatformHttpException(409, "duplicate", "shipping_service.code_duplicate");
        }

        var now = _clock.UtcNow;
        entity.Update(model.Code, model.ProviderKind, model.IconKey, model.ColorKey, model.SortOrder, now);
        entity.SetActive(model.IsActive, now);

        var existingTranslations = await _db.ShippingServiceTranslations
            .Where(x => x.ShippingServiceId == serviceId).ToListAsync(cancellationToken);
        _db.ShippingServiceTranslations.RemoveRange(existingTranslations);
        AddTranslations(serviceId, model.Translations);

        var existingOptions = await _db.ShippingServiceOptions
            .Where(x => x.ShippingServiceId == serviceId).ToListAsync(cancellationToken);
        var optionIds = existingOptions.Select(x => x.ShippingServiceOptionId).ToArray();
        var existingOptionTranslations = await _db.ShippingServiceOptionTranslations
            .Where(x => optionIds.Contains(x.ShippingServiceOptionId)).ToListAsync(cancellationToken);
        _db.ShippingServiceOptionTranslations.RemoveRange(existingOptionTranslations);
        _db.ShippingServiceOptions.RemoveRange(existingOptions);
        AddOptions(serviceId, model.Options, now);

        await _db.SaveChangesAsync(cancellationToken);
        return serviceId;
    }

    /// <inheritdoc />
    public async Task DeactivateAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        var entity = await _db.ShippingServices.FirstOrDefaultAsync(x => x.ShippingServiceId == serviceId, cancellationToken)
            ?? throw new PlatformHttpException(404, "not found", "shipping_service.not_found");
        entity.SetActive(false, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task EnsureSeedAsync(CancellationToken cancellationToken)
    {
        if (await _db.ShippingServices.AnyAsync(cancellationToken))
        {
            return;
        }

        var langs = await _languages.ListForSeedAsync(cancellationToken);
        if (langs.Count == 0)
        {
            return;
        }

        var fa = langs.FirstOrDefault(x => x.Culture.StartsWith("fa", StringComparison.OrdinalIgnoreCase)
                || x.Code.StartsWith("fa", StringComparison.OrdinalIgnoreCase))
            ?? langs.FirstOrDefault(x => x.IsDefault)
            ?? langs[0];
        var en = langs.FirstOrDefault(x => x.Culture.StartsWith("en", StringComparison.OrdinalIgnoreCase)
                || x.Code.StartsWith("en", StringComparison.OrdinalIgnoreCase));

        var now = _clock.UtcNow;
        void AddService(
            string code,
            string providerKind,
            string iconKey,
            string colorKey,
            int sort,
            string faName,
            string enName,
            (string code, string fa, string en)[] options)
        {
            var id = _ids.NewId();
            _db.ShippingServices.Add(ShippingService.Create(id, code, providerKind, iconKey, colorKey, sort, true, now));
            _db.ShippingServiceTranslations.Add(
                ShippingServiceTranslation.Create(_ids.NewId(), id, fa.LanguageId, faName, null));
            if (en is not null)
            {
                _db.ShippingServiceTranslations.Add(
                    ShippingServiceTranslation.Create(_ids.NewId(), id, en.LanguageId, enName, null));
            }

            var order = 10;
            foreach (var option in options)
            {
                var oid = _ids.NewId();
                _db.ShippingServiceOptions.Add(ShippingServiceOption.Create(oid, id, option.code, order, true, now));
                _db.ShippingServiceOptionTranslations.Add(
                    ShippingServiceOptionTranslation.Create(_ids.NewId(), oid, fa.LanguageId, option.fa));
                if (en is not null)
                {
                    _db.ShippingServiceOptionTranslations.Add(
                        ShippingServiceOptionTranslation.Create(_ids.NewId(), oid, en.LanguageId, option.en));
                }

                order += 10;
            }
        }

        AddService("post", "post", "post", "blue", 10, "پست", "Post",
        [
            ("express", "پیشتاز", "Express"),
            ("standard", "معمولی", "Standard"),
        ]);
        AddService("tipax", "tipax", "tipax", "amber", 20, "تیپاکس", "Tipax",
        [
            ("express", "پیشتاز", "Express"),
            ("standard", "معمولی", "Standard"),
        ]);
        AddService("snapp_courier", "courier", "courier", "emerald", 30, "اسنپ / پیک آنلاین", "Snapp / Online courier", []);
        AddService("store_courier", "store_courier", "bike", "violet", 40, "پیک فروشگاه", "Store courier", []);
        AddService("in_person", "in_person", "store", "rose", 50, "تحویل حضوری", "In person", []);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private void AddTranslations(Guid serviceId, IReadOnlyList<ShippingServiceTranslationWriteModel> translations)
    {
        foreach (var row in translations.Where(x => x.LanguageId != Guid.Empty && !string.IsNullOrWhiteSpace(x.Name)))
        {
            _db.ShippingServiceTranslations.Add(
                ShippingServiceTranslation.Create(_ids.NewId(), serviceId, row.LanguageId, row.Name, row.Description));
        }
    }

    private void AddOptions(
        Guid serviceId,
        IReadOnlyList<ShippingServiceOptionWriteModel> options,
        DateTimeOffset now)
    {
        foreach (var option in options)
        {
            if (string.IsNullOrWhiteSpace(option.Code))
            {
                continue;
            }

            var oid = option.ShippingServiceOptionId is { } existing && existing != Guid.Empty
                ? existing
                : _ids.NewId();
            _db.ShippingServiceOptions.Add(
                ShippingServiceOption.Create(oid, serviceId, option.Code, option.SortOrder, option.IsActive, now));
            foreach (var tr in option.Translations.Where(x => x.LanguageId != Guid.Empty && !string.IsNullOrWhiteSpace(x.Name)))
            {
                _db.ShippingServiceOptionTranslations.Add(
                    ShippingServiceOptionTranslation.Create(_ids.NewId(), oid, tr.LanguageId, tr.Name));
            }
        }
    }

    private async Task ValidateLanguagesAsync(ShippingServiceWriteModel model, CancellationToken cancellationToken)
    {
        var ids = model.Translations.Select(t => t.LanguageId)
            .Concat(model.Options.SelectMany(o => o.Translations.Select(t => t.LanguageId)))
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        await _languages.EnsureKnownAsync(ids, cancellationToken);
    }
}
