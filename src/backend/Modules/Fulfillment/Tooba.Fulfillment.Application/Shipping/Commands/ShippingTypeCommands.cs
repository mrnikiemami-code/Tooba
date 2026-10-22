using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Fulfillment.Application.Shipping.Models;
using Tooba.Fulfillment.Application.Composition;
using Tooba.Fulfillment.Contracts.Errors;
using Tooba.Fulfillment.Domain.Aggregates;

namespace Tooba.Fulfillment.Application.Shipping.Commands;

/// <summary>ایجاد یک تعریف نوع ارسال غیرفعال.</summary>
/// <param name="Model">ورودی حضور‌آگاه transport؛ اعتبارسنجی کامل پیش از هر تغییر tracked.</param>
public sealed record CreateShippingTypeCommand(ShippingTypeWriteModel Model)
    : IRequest<Result<ShippingTypeView>>;

/// <summary>
/// مسیر ایجاد: پیش‌بررسی زبان/قواعد و نرمال‌سازی کل ورودی پیش از هر تغییر، سپس یک SaveChanges.
/// </summary>
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

            var languages = (await _languageFacts.ReadAsync(cancellationToken)).Resolve();
            var code = ShippingTypePreparation.RequireCode(request.Model.Code);
            var sortOrder = ShippingTypePreparation.RequireSortOrder(request.Model.SortOrder);

            // ورودی null پیش از هر نرمال‌سازی رد می‌شود؛ هیچ فهرست خالی جایگزین نمی‌شود.
            if (request.Model.Translations is null)
            {
                throw new SemanticException(new SemanticError(FulfillmentErrorCodes.ShippingTypeTranslationInvalid));
            }

            var translations = ShippingTypePreparation.Prepare(request.Model.Translations, languages);

            // تعریف تازه باید نام زبان پیش‌فرض فعال را صریح داشته باشد؛ هیچ ترجمهٔ حدسی ساخته نمی‌شود.
            if (!translations.Any(x => x.LanguageId == languages.ActiveDefaultLanguageId))
            {
                throw new SemanticException(new SemanticError(
                    FulfillmentErrorCodes.ShippingTypeDefaultLanguageNameRequired));
            }

            var id = await _store.CreateAsync(code, sortOrder, translations, cancellationToken);

            return await _store.GetAsync(id, languages.ActiveDefaultLanguageId, cancellationToken)
                   ?? throw new SemanticException(new SemanticError(FulfillmentErrorCodes.ShippingTypeNotFound));
        });
}

/// <summary>ویرایش تعریف: ترتیب/فعال‌بودن و patch متن زبان‌های صریح.</summary>
/// <param name="ShippingTypeId">شناسهٔ هدف.</param>
/// <param name="Model">ورودی حضور‌آگاه transport.</param>
public sealed record UpdateShippingTypeCommand(Guid ShippingTypeId, ShippingTypeWriteModel Model)
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

            var languages = (await _languageFacts.ReadAsync(cancellationToken)).Resolve();
            var sortOrder = ShippingTypePreparation.RequireSortOrder(request.Model.SortOrder);

            if (request.Model.IsActive is not { } isActive || request.Model.ExpectedRevision is not { } expected)
            {
                throw new SemanticException(new SemanticError(FulfillmentErrorCodes.ShippingTypeTransportFieldMissing));
            }

            if (request.Model.Translations is null)
            {
                throw new SemanticException(new SemanticError(FulfillmentErrorCodes.ShippingTypeTranslationInvalid));
            }

            var translations = ShippingTypePreparation.Prepare(request.Model.Translations, languages);

            return await _store.UpdateAsync(
                request.ShippingTypeId,
                expected,
                request.Model.Code,
                sortOrder,
                isActive,
                translations,
                languages.ActiveDefaultLanguageId,
                cancellationToken);
        });
}
