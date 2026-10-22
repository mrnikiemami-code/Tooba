using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Composition;
using Tooba.Fulfillment.Contracts.Errors;

namespace Tooba.Fulfillment.Application.ShippingTypes;

/// <summary>
/// پیش‌بررسی مشترک نوشتن نوع ارسال روی حقایق زبان authoritative و شکل ورودی ترجمه‌ها.
/// </summary>
/// <remarks>
/// این کلاس در Application است، نه Domain: حقایق زبان از یک درز خارجی خوانده می‌شوند و Domain فقط
/// واقعیت‌های محلی آماده‌شده را اعتبارسنجی می‌کند. هیچ زبان/پیش‌فرضی از locale درخواست ساخته نمی‌شود.
/// </remarks>
internal static class ShippingTypeWorkflow
{
    /// <summary>
    /// ورودی ترجمه‌ها را نرمال می‌کند: بدون زبان تکراری/تهی، بدون ردیف null و فقط زبان فعال شناخته‌شده.
    /// </summary>
    internal static IReadOnlyList<ShippingTypeTranslationInput> NormalizeTranslations(
        IReadOnlyList<ShippingTypeTranslationInput>? translations,
        ShippingTypeLanguageFacts facts)
    {
        if (translations is null)
        {
            throw Fault(FulfillmentErrorCodes.ShippingTypeTranslationInvalid);
        }

        var seen = new HashSet<Guid>();
        var normalized = new List<ShippingTypeTranslationInput>(translations.Count);
        foreach (var entry in translations)
        {
            if (entry.LanguageId == Guid.Empty || !seen.Add(entry.LanguageId) || !facts.IsActive(entry.LanguageId))
            {
                throw Fault(FulfillmentErrorCodes.ShippingTypeLanguageInvalid);
            }

            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                throw Fault(FulfillmentErrorCodes.ShippingTypeTranslationInvalid);
            }

            normalized.Add(entry);
        }

        return normalized;
    }

    /// <summary>رجیستری زبان باید یک پیش‌فرض فعال قابل‌استفاده داشته باشد.</summary>
    internal static ShippingTypeLanguageFact RequireActiveDefault(ShippingTypeLanguageFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts.ActiveDefault
               ?? throw Fault(FulfillmentErrorCodes.ShippingTypeDefaultLanguageUnavailable);
    }

    private static SemanticException Fault(string code) => new(new SemanticError(code));
}

/// <summary>ایجاد یک تعریف نوع ارسال غیرفعال.</summary>
/// <param name="Model">مدل نوشتن.</param>
public sealed record CreateShippingTypeCommand(ShippingTypeCreateModel Model)
    : IRequest<Result<ShippingTypeView>>;

/// <summary>مسیر ایجاد: پیش‌بررسی زبان/قواعد پیش از هر تغییر tracked، سپس یک SaveChanges.</summary>
public sealed class CreateShippingTypeHandler
    : IRequestHandler<CreateShippingTypeCommand, Result<ShippingTypeView>>
{
    private readonly IShippingTypeStore _store;
    private readonly IShippingTypeLanguageFacts _languageFacts;

    /// <summary>سازنده.</summary>
    public CreateShippingTypeHandler(IShippingTypeStore store, IShippingTypeLanguageFacts languageFacts)
    {
        _store = store;
        _languageFacts = languageFacts;
    }

    /// <inheritdoc />
    public Task<Result<ShippingTypeView>> Handle(
        CreateShippingTypeCommand request,
        CancellationToken cancellationToken) =>
        FulfillmentOperation.ExecuteAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(request.Model);

            var facts = await _languageFacts.ReadAsync(cancellationToken);
            var defaultLanguage = ShippingTypeWorkflow.RequireActiveDefault(facts);
            var translations = ShippingTypeWorkflow.NormalizeTranslations(request.Model.Translations, facts);

            // تعریف تازه باید نام زبان پیش‌فرض فعال را صریح داشته باشد؛ هیچ پیش‌فرض/ترجمهٔ حدسی ساخته نمی‌شود.
            if (!translations.Any(x => x.LanguageId == defaultLanguage.LanguageId))
            {
                throw new SemanticException(new SemanticError(
                    FulfillmentErrorCodes.ShippingTypeDefaultLanguageNameRequired));
            }

            var id = await _store.CreateAsync(
                request.Model with { Translations = translations },
                cancellationToken);

            return await _store.GetAsync(id, cancellationToken)
                   ?? throw new SemanticException(new SemanticError(FulfillmentErrorCodes.ShippingTypeNotFound));
        });
}

/// <summary>ویرایش تعریف: ترتیب/فعال‌بودن و patch متن زبان‌های صریح.</summary>
/// <param name="ShippingTypeId">شناسهٔ هدف.</param>
/// <param name="Model">مدل نوشتن.</param>
public sealed record UpdateShippingTypeCommand(Guid ShippingTypeId, ShippingTypeUpdateModel Model)
    : IRequest<Result<ShippingTypeView>>;

/// <summary>مسیر ویرایش با preflight کامل پیش از تغییر tracked و predicate بازبینی واقعی.</summary>
public sealed class UpdateShippingTypeHandler
    : IRequestHandler<UpdateShippingTypeCommand, Result<ShippingTypeView>>
{
    private readonly IShippingTypeStore _store;
    private readonly IShippingTypeLanguageFacts _languageFacts;

    /// <summary>سازنده.</summary>
    public UpdateShippingTypeHandler(IShippingTypeStore store, IShippingTypeLanguageFacts languageFacts)
    {
        _store = store;
        _languageFacts = languageFacts;
    }

    /// <inheritdoc />
    public Task<Result<ShippingTypeView>> Handle(
        UpdateShippingTypeCommand request,
        CancellationToken cancellationToken) =>
        FulfillmentOperation.ExecuteAsync(async () =>
        {
            ArgumentNullException.ThrowIfNull(request.Model);

            var facts = await _languageFacts.ReadAsync(cancellationToken);
            ShippingTypeWorkflow.RequireActiveDefault(facts);
            var translations = ShippingTypeWorkflow.NormalizeTranslations(request.Model.Translations, facts);

            // فعال‌سازی/فعال‌نگه‌داشتن باید نام زبان پیش‌فرض فعال را در وضعیت ذخیره‌شده یا همین patch داشته باشد.
            return await _store.UpdateAsync(
                request.ShippingTypeId,
                request.Model with { Translations = translations },
                cancellationToken);
        });
}
