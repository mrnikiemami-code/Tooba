using Tooba.Shipping.Contracts.Carriers;
using Tooba.Shipping.Domain.Authoring;

#pragma warning disable CS1591 // Carrier domain types are documented at the aggregate level.

namespace Tooba.Shipping.Domain.Carriers;

/// <summary>
/// حساب حامل مالک‌محور. راز فقط به‌صورت ارجاع نگهداری می‌شود؛ مقدار راز در این aggregate نیست.
/// </summary>
public sealed class ShippingCarrierAccount
{
    public const int MaxProviderCodeLength = 64;
    public const int MaxDisplayNameLength = 128;
    public const int MaxSecretReferenceLength = 128;
    public const int MaxUrlLength = 256;
    public const int MaxCurrencyLength = 3;

    private ShippingCarrierAccount()
    {
    }

    public Guid AccountId { get; private set; }

    public ShippingDraftOwnerKind OwnerKind { get; private set; }

    public string? OwnerTenantId { get; private set; }

    public Guid? OwnerSellerPartyId { get; private set; }

    public string ProviderCode { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public ShippingCarrierEnvironment Environment { get; private set; }

    public string SecretReference { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public int ConfigurationVersion { get; private set; }

    public string? ApiBaseUrl { get; private set; }

    public string? TokenUrl { get; private set; }

    public string CurrencyCode { get; private set; } = "IRR";

    public ShippingCarrierConnectionTestOutcome? LastConnectionOutcome { get; private set; }

    public DateTimeOffset? LastConnectionAt { get; private set; }

    public DateTimeOffset? LastReferenceSyncAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ShippingCarrierAccount Create(
        Guid accountId,
        ShippingDraftOwnerKind ownerKind,
        string? ownerTenantId,
        Guid? ownerSellerPartyId,
        string providerCode,
        string displayName,
        ShippingCarrierEnvironment environment,
        string secretReference,
        bool isActive,
        string? apiBaseUrl,
        string? tokenUrl,
        string? currencyCode,
        DateTimeOffset now)
    {
        var account = new ShippingCarrierAccount
        {
            AccountId = accountId == Guid.Empty ? Guid.NewGuid() : accountId,
            OwnerKind = ownerKind,
            OwnerTenantId = NormalizeTenant(ownerTenantId),
            OwnerSellerPartyId = ownerSellerPartyId,
            CreatedAt = now,
            UpdatedAt = now,
            ConfigurationVersion = 1,
        };
        account.ApplyProfile(providerCode, displayName, environment, secretReference, isActive, apiBaseUrl, tokenUrl, currencyCode, now, bumpVersion: false);
        return account;
    }

    public void ReplaceProfile(
        string providerCode,
        string displayName,
        ShippingCarrierEnvironment environment,
        string secretReference,
        bool isActive,
        string? apiBaseUrl,
        string? tokenUrl,
        string? currencyCode,
        DateTimeOffset now)
        => ApplyProfile(providerCode, displayName, environment, secretReference, isActive, apiBaseUrl, tokenUrl, currencyCode, now, bumpVersion: true);

    public void RecordConnectionResult(ShippingCarrierConnectionTestOutcome outcome, DateTimeOffset now)
    {
        LastConnectionOutcome = outcome;
        LastConnectionAt = now;
        UpdatedAt = now;
    }

    public void RecordReferenceSync(DateTimeOffset now)
    {
        LastReferenceSyncAt = now;
        UpdatedAt = now;
    }

    public void BumpConfigurationVersion(DateTimeOffset now)
    {
        ConfigurationVersion = checked(ConfigurationVersion + 1);
        UpdatedAt = now;
    }

    public bool BelongsTo(ShippingDraftOwnerKind ownerKind, string? ownerTenantId, Guid? ownerSellerPartyId)
        => OwnerKind == ownerKind
           && string.Equals(OwnerTenantId, NormalizeTenant(ownerTenantId), StringComparison.Ordinal)
           && OwnerSellerPartyId == ownerSellerPartyId;

    private void ApplyProfile(
        string providerCode,
        string displayName,
        ShippingCarrierEnvironment environment,
        string secretReference,
        bool isActive,
        string? apiBaseUrl,
        string? tokenUrl,
        string? currencyCode,
        DateTimeOffset now,
        bool bumpVersion)
    {
        if (!Enum.IsDefined(environment))
        {
            throw new ArgumentOutOfRangeException(nameof(environment));
        }

        ProviderCode = NormalizeRequired(providerCode, MaxProviderCodeLength).ToLowerInvariant();
        DisplayName = NormalizeRequired(displayName, MaxDisplayNameLength);
        Environment = environment;
        SecretReference = NormalizeRequired(secretReference, MaxSecretReferenceLength);
        IsActive = isActive;
        ApiBaseUrl = NormalizeOptional(apiBaseUrl, MaxUrlLength);
        TokenUrl = NormalizeOptional(tokenUrl, MaxUrlLength);
        CurrencyCode = NormalizeCurrency(currencyCode);
        UpdatedAt = now;
        if (bumpVersion)
        {
            ConfigurationVersion = checked(ConfigurationVersion + 1);
        }
    }

    private static string NormalizeRequired(string value, int max)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0 || trimmed.Length > max)
        {
            throw new ArgumentException("Carrier account field is invalid.", nameof(value));
        }

        return trimmed;
    }

    private static string? NormalizeOptional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > max ? throw new ArgumentException("Carrier account field is too long.", nameof(value)) : trimmed;
    }

    private static string? NormalizeTenant(string? tenantId)
        => string.IsNullOrWhiteSpace(tenantId) ? null : tenantId.Trim();

    private static string NormalizeCurrency(string? currency)
    {
        var normalized = (currency ?? "IRR").Trim().ToUpperInvariant();
        if (normalized.Length != MaxCurrencyLength || !normalized.All(char.IsAsciiLetterUpper))
        {
            throw new ArgumentException("Currency is invalid.", nameof(currency));
        }

        return normalized;
    }
}
