using Tooba.Shipping.Contracts.Carriers;

#pragma warning disable CS1591 // Carrier domain types are documented at the aggregate level.

namespace Tooba.Shipping.Domain.Carriers;

/// <summary>مادهٔ راز رمزنگاری‌شدهٔ سمت سرور؛ فقط با ارجاع در حساب حمل اشاره می‌شود.</summary>
public sealed class ShippingCarrierSecretMaterial
{
    public const int MaxCipherLength = 8192;

    private ShippingCarrierSecretMaterial()
    {
    }

    public string SecretReference { get; private set; } = string.Empty;

    public byte[] Ciphertext { get; private set; } = Array.Empty<byte>();

    public int ConfigurationVersion { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ShippingCarrierSecretMaterial Create(
        string secretReference,
        byte[] ciphertext,
        int configurationVersion,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretReference);
        ArgumentNullException.ThrowIfNull(ciphertext);
        if (ciphertext.Length == 0 || ciphertext.Length > MaxCipherLength)
        {
            throw new ArgumentException("Ciphertext length is invalid.", nameof(ciphertext));
        }

        return new ShippingCarrierSecretMaterial
        {
            SecretReference = secretReference.Trim(),
            Ciphertext = ciphertext,
            ConfigurationVersion = configurationVersion,
            UpdatedAt = now,
        };
    }

    public void Replace(byte[] ciphertext, int configurationVersion, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        if (ciphertext.Length == 0 || ciphertext.Length > MaxCipherLength)
        {
            throw new ArgumentException("Ciphertext length is invalid.", nameof(ciphertext));
        }

        Ciphertext = ciphertext;
        ConfigurationVersion = configurationVersion;
        UpdatedAt = now;
    }
}

/// <summary>یک مقدار مرجع همگام‌شده از provider برای یک حساب.</summary>
public sealed class ShippingCarrierReferenceValue
{
    public const int MaxExternalIdLength = 64;
    public const int MaxTitleLength = 256;

    private ShippingCarrierReferenceValue()
    {
    }

    public Guid AccountId { get; private set; }

    public ShippingCarrierReferenceKind Kind { get; private set; }

    public string ExternalId { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public DateTimeOffset SyncedAt { get; private set; }

    public static ShippingCarrierReferenceValue Create(
        Guid accountId,
        ShippingCarrierReferenceKind kind,
        string externalId,
        string title,
        bool isActive,
        DateTimeOffset now)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Account id is required.", nameof(accountId));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        return new ShippingCarrierReferenceValue
        {
            AccountId = accountId,
            Kind = kind,
            ExternalId = Normalize(externalId, MaxExternalIdLength),
            Title = Normalize(title, MaxTitleLength),
            IsActive = isActive,
            SyncedAt = now,
        };
    }

    public void Refresh(string title, bool isActive, DateTimeOffset now)
    {
        Title = Normalize(title, MaxTitleLength);
        IsActive = isActive;
        SyncedAt = now;
    }

    private static string Normalize(string value, int max)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0 || trimmed.Length > max)
        {
            throw new ArgumentException("Reference value is invalid.", nameof(value));
        }

        return trimmed;
    }
}

/// <summary>نگاشت یکتای شهر Geography به کد خارجی حامل برای یک حساب.</summary>
public sealed class ShippingCarrierDestinationMapping
{
    public const int MaxExternalCodeLength = 64;
    public const int MaxProvenanceLength = 128;

    private ShippingCarrierDestinationMapping()
    {
    }

    public Guid MappingId { get; private set; }

    public Guid AccountId { get; private set; }

    public string ProviderCode { get; private set; } = string.Empty;

    public Guid GeographyCityId { get; private set; }

    public string ExternalLocationCode { get; private set; } = string.Empty;

    public string Provenance { get; private set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ShippingCarrierDestinationMapping Create(
        Guid mappingId,
        Guid accountId,
        string providerCode,
        Guid geographyCityId,
        string externalLocationCode,
        string provenance,
        DateTimeOffset now)
    {
        if (accountId == Guid.Empty || geographyCityId == Guid.Empty)
        {
            throw new ArgumentException("Mapping identities are required.");
        }

        return new ShippingCarrierDestinationMapping
        {
            MappingId = mappingId == Guid.Empty ? Guid.NewGuid() : mappingId,
            AccountId = accountId,
            ProviderCode = (providerCode ?? string.Empty).Trim().ToLowerInvariant(),
            GeographyCityId = geographyCityId,
            ExternalLocationCode = Normalize(externalLocationCode, MaxExternalCodeLength),
            Provenance = Normalize(provenance, MaxProvenanceLength),
            UpdatedAt = now,
        };
    }

    public void Replace(string externalLocationCode, string provenance, DateTimeOffset now)
    {
        ExternalLocationCode = Normalize(externalLocationCode, MaxExternalCodeLength);
        Provenance = Normalize(provenance, MaxProvenanceLength);
        UpdatedAt = now;
    }

    private static string Normalize(string value, int max)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0 || trimmed.Length > max)
        {
            throw new ArgumentException("Mapping field is invalid.", nameof(value));
        }

        return trimmed;
    }
}

/// <summary>اتصال CarrierApi روی یک پیش‌نویس؛ شناسه‌های مرجع حدس زده نمی‌شوند.</summary>
public sealed class ShippingCarrierMethodBinding
{
    public const int MaxIdLength = 64;
    public const int MaxAddressLength = 512;

    private ShippingCarrierMethodBinding()
    {
    }

    public Guid BindingId { get; private set; }

    public Guid DraftId { get; private set; }

    public Guid CarrierAccountId { get; private set; }

    public string ProviderCode { get; private set; } = string.Empty;

    public string ProviderServiceId { get; private set; } = string.Empty;

    public string PaymentTypeId { get; private set; } = string.Empty;

    public string PickupTypeId { get; private set; } = string.Empty;

    public string DistributionTypeId { get; private set; } = string.Empty;

    public string PackingId { get; private set; } = string.Empty;

    public string PackageContentId { get; private set; } = string.Empty;

    public string PackTypeId { get; private set; } = string.Empty;

    public string OriginExternalCityCode { get; private set; } = string.Empty;

    public string? OriginAddressLine { get; private set; }

    public decimal? OriginLatitude { get; private set; }

    public decimal? OriginLongitude { get; private set; }

    public decimal DefaultLengthCentimeters { get; private set; }

    public decimal DefaultWidthCentimeters { get; private set; }

    public decimal DefaultHeightCentimeters { get; private set; }

    public int ConfigurationVersion { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static ShippingCarrierMethodBinding Create(
        Guid bindingId,
        Guid draftId,
        Guid carrierAccountId,
        string providerCode,
        string providerServiceId,
        string paymentTypeId,
        string pickupTypeId,
        string distributionTypeId,
        string packingId,
        string packageContentId,
        string packTypeId,
        string originExternalCityCode,
        string? originAddressLine,
        decimal? originLatitude,
        decimal? originLongitude,
        decimal defaultLengthCentimeters,
        decimal defaultWidthCentimeters,
        decimal defaultHeightCentimeters,
        int configurationVersion,
        DateTimeOffset now)
    {
        if (draftId == Guid.Empty || carrierAccountId == Guid.Empty)
        {
            throw new ArgumentException("Binding identities are required.");
        }

        EnsurePositiveDimension(defaultLengthCentimeters);
        EnsurePositiveDimension(defaultWidthCentimeters);
        EnsurePositiveDimension(defaultHeightCentimeters);

        return new ShippingCarrierMethodBinding
        {
            BindingId = bindingId == Guid.Empty ? Guid.NewGuid() : bindingId,
            DraftId = draftId,
            CarrierAccountId = carrierAccountId,
            ProviderCode = (providerCode ?? string.Empty).Trim().ToLowerInvariant(),
            ProviderServiceId = NormalizeId(providerServiceId),
            PaymentTypeId = NormalizeId(paymentTypeId),
            PickupTypeId = NormalizeId(pickupTypeId),
            DistributionTypeId = NormalizeId(distributionTypeId),
            PackingId = NormalizeId(packingId),
            PackageContentId = NormalizeId(packageContentId),
            PackTypeId = NormalizeId(packTypeId),
            OriginExternalCityCode = NormalizeId(originExternalCityCode),
            OriginAddressLine = string.IsNullOrWhiteSpace(originAddressLine) ? null : originAddressLine.Trim(),
            OriginLatitude = originLatitude,
            OriginLongitude = originLongitude,
            DefaultLengthCentimeters = defaultLengthCentimeters,
            DefaultWidthCentimeters = defaultWidthCentimeters,
            DefaultHeightCentimeters = defaultHeightCentimeters,
            ConfigurationVersion = configurationVersion,
            UpdatedAt = now,
        };
    }

    public void Replace(
        Guid carrierAccountId,
        string providerCode,
        string providerServiceId,
        string paymentTypeId,
        string pickupTypeId,
        string distributionTypeId,
        string packingId,
        string packageContentId,
        string packTypeId,
        string originExternalCityCode,
        string? originAddressLine,
        decimal? originLatitude,
        decimal? originLongitude,
        decimal defaultLengthCentimeters,
        decimal defaultWidthCentimeters,
        decimal defaultHeightCentimeters,
        int configurationVersion,
        DateTimeOffset now)
    {
        if (carrierAccountId == Guid.Empty)
        {
            throw new ArgumentException("Carrier account is required.", nameof(carrierAccountId));
        }

        EnsurePositiveDimension(defaultLengthCentimeters);
        EnsurePositiveDimension(defaultWidthCentimeters);
        EnsurePositiveDimension(defaultHeightCentimeters);

        CarrierAccountId = carrierAccountId;
        ProviderCode = (providerCode ?? string.Empty).Trim().ToLowerInvariant();
        ProviderServiceId = NormalizeId(providerServiceId);
        PaymentTypeId = NormalizeId(paymentTypeId);
        PickupTypeId = NormalizeId(pickupTypeId);
        DistributionTypeId = NormalizeId(distributionTypeId);
        PackingId = NormalizeId(packingId);
        PackageContentId = NormalizeId(packageContentId);
        PackTypeId = NormalizeId(packTypeId);
        OriginExternalCityCode = NormalizeId(originExternalCityCode);
        OriginAddressLine = string.IsNullOrWhiteSpace(originAddressLine) ? null : originAddressLine.Trim();
        OriginLatitude = originLatitude;
        OriginLongitude = originLongitude;
        DefaultLengthCentimeters = defaultLengthCentimeters;
        DefaultWidthCentimeters = defaultWidthCentimeters;
        DefaultHeightCentimeters = defaultHeightCentimeters;
        ConfigurationVersion = configurationVersion;
        UpdatedAt = now;
    }

    private static string NormalizeId(string value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0 || trimmed.Length > MaxIdLength)
        {
            throw new ArgumentException("Provider reference id is invalid.", nameof(value));
        }

        return trimmed;
    }

    private static void EnsurePositiveDimension(decimal value)
    {
        if (value <= 0m)
        {
            throw new ArgumentException("Package dimension must be positive.");
        }
    }
}
