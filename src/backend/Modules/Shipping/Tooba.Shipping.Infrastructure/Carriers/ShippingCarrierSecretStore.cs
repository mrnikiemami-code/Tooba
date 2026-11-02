using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Domain.Carriers;
using Tooba.Shipping.Infrastructure.Persistence;

namespace Tooba.Shipping.Infrastructure.Carriers;

/// <summary>
/// راز حامل با ASP.NET DataProtection؛ فقط ciphertext در schemaی shipping ذخیره می‌شود.
/// </summary>
internal sealed class ShippingCarrierSecretStore(
    ShippingDbContext db,
    IDataProtectionProvider dataProtection) : IShippingCarrierSecretStore
{
    private const string Purpose = "Tooba.Shipping.CarrierSecrets.v1";
    private readonly IDataProtector _protector = dataProtection.CreateProtector(Purpose);

    public async Task PutAsync(
        string secretReference,
        string username,
        string password,
        int configurationVersion,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var payload = JsonSerializer.Serialize(new SecretPayload(username.Trim(), password));
        var ciphertext = _protector.Protect(Encoding.UTF8.GetBytes(payload));
        var now = DateTimeOffset.UtcNow;
        var existing = await db.CarrierSecretMaterials
            .SingleOrDefaultAsync(x => x.SecretReference == secretReference.Trim(), cancellationToken);
        if (existing is null)
        {
            db.CarrierSecretMaterials.Add(
                ShippingCarrierSecretMaterial.Create(secretReference.Trim(), ciphertext, configurationVersion, now));
        }
        else
        {
            existing.Replace(ciphertext, configurationVersion, now);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<ShippingCarrierSecretMaterialPayload?> TryGetAsync(
        string secretReference,
        int expectedConfigurationVersion,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secretReference))
        {
            return null;
        }

        var row = await db.CarrierSecretMaterials
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.SecretReference == secretReference.Trim(), cancellationToken);
        if (row is null || row.ConfigurationVersion != expectedConfigurationVersion)
        {
            return null;
        }

        try
        {
            var bytes = _protector.Unprotect(row.Ciphertext);
            var payload = JsonSerializer.Deserialize<SecretPayload>(Encoding.UTF8.GetString(bytes));
            if (payload is null
                || string.IsNullOrWhiteSpace(payload.Username)
                || string.IsNullOrWhiteSpace(payload.Password))
            {
                return null;
            }

            return new ShippingCarrierSecretMaterialPayload(
                payload.Username,
                payload.Password,
                row.ConfigurationVersion);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public async Task InvalidateAsync(string secretReference, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secretReference))
        {
            return;
        }

        var row = await db.CarrierSecretMaterials
            .SingleOrDefaultAsync(x => x.SecretReference == secretReference.Trim(), cancellationToken);
        if (row is null)
        {
            return;
        }

        db.CarrierSecretMaterials.Remove(row);
        await db.SaveChangesAsync(cancellationToken);
    }

    private sealed record SecretPayload(string Username, string Password);
}
