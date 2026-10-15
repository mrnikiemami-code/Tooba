using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;

namespace Tooba.Fulfillment.Domain.Aggregates;

/// <summary>ترجمهٔ نام روش ارسال یک‌سطحی.</summary>
public sealed class ShippingMethodTranslation
{
    /// <summary>شناسه ترجمه.</summary>
    public Guid TranslationId { get; init; }

    /// <summary>روش مالک.</summary>
    public Guid ShippingMethodId { get; init; }

    /// <summary>زبان رجیستری.</summary>
    public Guid LanguageId { get; init; }

    /// <summary>نام محلی.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>توضیح اختیاری.</summary>
    public string? Description { get; private set; }

    /// <summary>ترجمه می‌سازد.</summary>
    public static ShippingMethodTranslation Create(
        Guid translationId,
        Guid shippingMethodId,
        Guid languageId,
        string name,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new SemanticException(new SemanticError("shipping_service.name.required"));
        }

        return new ShippingMethodTranslation
        {
            TranslationId = translationId,
            ShippingMethodId = shippingMethodId,
            LanguageId = languageId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
        };
    }
}
