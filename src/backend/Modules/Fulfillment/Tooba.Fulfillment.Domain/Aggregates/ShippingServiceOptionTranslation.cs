

namespace Tooba.Fulfillment.Domain.Aggregates;


/// <summary>ترجمهٔ نوع سرویس فرزند.</summary>
public sealed class ShippingServiceOptionTranslation
{
    /// <summary>شناسه ترجمه.</summary>
    public Guid TranslationId { get; init; }

    /// <summary>گزینه مالک.</summary>
    public Guid ShippingServiceOptionId { get; init; }

    /// <summary>زبان رجیستری.</summary>
    public Guid LanguageId { get; init; }

    /// <summary>نام محلی.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>ترجمه می‌سازد.</summary>
    public static ShippingServiceOptionTranslation Create(
        Guid translationId,
        Guid optionId,
        Guid languageId,
        string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("shipping_service_option.name.required");
        }

        return new ShippingServiceOptionTranslation
        {
            TranslationId = translationId,
            ShippingServiceOptionId = optionId,
            LanguageId = languageId,
            Name = name.Trim(),
        };
    }

    /// <summary>نام را به‌روز می‌کند.</summary>
    public void Update(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("shipping_service_option.name.required");
        }

        Name = name.Trim();
    }
}
