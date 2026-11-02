namespace Tooba.Shipping.Application.Carriers.Ports;

/// <summary>
/// مرز راز حامل: فقط ارجاع + ciphertext. مقدار راز هرگز در DTO عمومی یا لاگ نیست.
/// </summary>
public interface IShippingCarrierSecretStore
{
    Task PutAsync(
        string secretReference,
        string username,
        string password,
        int configurationVersion,
        CancellationToken cancellationToken);

    Task<ShippingCarrierSecretMaterialPayload?> TryGetAsync(
        string secretReference,
        int expectedConfigurationVersion,
        CancellationToken cancellationToken);

    Task InvalidateAsync(string secretReference, CancellationToken cancellationToken);
}

/// <summary>مواد راز بازیابی‌شده برای اجرای provider؛ فقط داخل Infrastructure مصرف می‌شود.</summary>
public sealed record ShippingCarrierSecretMaterialPayload(
    string Username,
    string Password,
    int ConfigurationVersion);
