

namespace Tooba.Fulfillment.Domain.Aggregates;


/// <summary>ترجمهٔ نام سرویس ارسال والد.</summary>
public sealed class ShippingServiceTranslation
{
    /// <summary>شناسه ترجمه.</summary>
    public Guid TranslationId { get; init; }

    /// <summary>سرویس مالک.</summary>
    public Guid ShippingServiceId { get; init; }

    /// <summary>زبان رجیستری.</summary>
    public Guid LanguageId { get; init; }

    /// <summary>نام محلی.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>توضیح اختیاری.</summary>
    public string? Description { get; private set; }

    /// <summary>ترجمه می‌سازد.</summary>
    public static ShippingServiceTranslation Create(
        Guid translationId,
        Guid shippingServiceId,
        Guid languageId,
        string name,
        string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("shipping_service.name.required");
        }

        return new ShippingServiceTranslation
        {
            TranslationId = translationId,
            ShippingServiceId = shippingServiceId,
            LanguageId = languageId,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
        };
    }

    /// <summary>نام/توضیح را به‌روز می‌کند.</summary>
    public void Update(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("shipping_service.name.required");
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}
