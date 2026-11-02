using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Authoring.Models;
using Tooba.Shipping.Application.Carriers;
using Tooba.Shipping.Application.Carriers.Ports;
using Tooba.Shipping.Contracts.Carriers;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Authoring;
using Tooba.Shipping.Domain.Carriers;
using Tooba.Shipping.Infrastructure.Carriers.Tipax;
using Tooba.Shipping.Infrastructure.Persistence;

namespace Tooba.Shipping.Infrastructure.Carriers;

/// <summary>درز مالک‌محور حساب/مرجع/نگاشت/اتصال CarrierApi.</summary>
internal sealed class ShippingCarrierAccountDirectory(
    ShippingDbContext db,
    IShippingCarrierSecretStore secrets,
    TipaxV4AccountClient tipax) : IShippingCarrierAccountDirectory
{
    public async Task<Result<IReadOnlyList<ShippingCarrierAccountView>>> ListAccountsAsync(
        ShippingAuthoringOwnerScope owner,
        CancellationToken cancellationToken)
    {
        var key = ToDomainOwner(owner);
        var rows = await OwnedAccounts(key).AsNoTracking().OrderBy(x => x.DisplayName).ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ShippingCarrierAccountView>>(rows.Select(MapAccount).ToList());
    }

    public async Task<Result<ShippingCarrierAccountView>> GetAccountAsync(
        ShippingAuthoringOwnerScope owner,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await FindOwnedAsync(owner, accountId, cancellationToken);
        return account is null
            ? Result.Failure<ShippingCarrierAccountView>(new SemanticError(ShippingErrorCodes.CarrierAccountNotFound))
            : Result.Success(MapAccount(account));
    }

    public async Task<Result<ShippingCarrierAccountView>> UpsertAccountAsync(
        ShippingAuthoringOwnerScope owner,
        Guid? accountId,
        ShippingCarrierAccountUpsertRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var key = ToDomainOwner(owner);
        var now = DateTimeOffset.UtcNow;
        if (!string.Equals(request.ProviderCode?.Trim(), "tipax", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ShippingCarrierAccountView>(
                new SemanticError(ShippingErrorCodes.CarrierProviderUnknown));
        }

        ShippingCarrierAccount account;
        if (accountId is { } existingId)
        {
            account = await FindOwnedAsync(owner, existingId, cancellationToken)
                      ?? throw new SemanticException(new SemanticError(ShippingErrorCodes.CarrierAccountNotFound));
            var secretReference = account.SecretReference;
            var previousVersion = account.ConfigurationVersion;
            var previousSecret = await secrets.TryGetAsync(secretReference, previousVersion, cancellationToken);
            account.ReplaceProfile(
                request.ProviderCode,
                request.DisplayName,
                request.Environment,
                secretReference,
                request.IsActive,
                request.ApiBaseUrl,
                request.TokenUrl,
                request.CurrencyCode,
                now);
            if (!string.IsNullOrWhiteSpace(request.Username) && !string.IsNullOrWhiteSpace(request.Password))
            {
                await secrets.PutAsync(
                    secretReference,
                    request.Username,
                    request.Password,
                    account.ConfigurationVersion,
                    cancellationToken);
            }
            else if (previousSecret is not null)
            {
                await secrets.PutAsync(
                    secretReference,
                    previousSecret.Username,
                    previousSecret.Password,
                    account.ConfigurationVersion,
                    cancellationToken);
            }

            tipax.InvalidateToken(account.AccountId, previousVersion);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Result.Failure<ShippingCarrierAccountView>(
                    new SemanticError(ShippingErrorCodes.CarrierSecretRequired));
            }

            var newId = Guid.NewGuid();
            var secretReference = $"shipping.carrier.{newId:N}";
            account = ShippingCarrierAccount.Create(
                newId,
                key.Kind,
                key.TenantId,
                key.SellerPartyId,
                request.ProviderCode,
                request.DisplayName,
                request.Environment,
                secretReference,
                request.IsActive,
                request.ApiBaseUrl,
                request.TokenUrl,
                request.CurrencyCode,
                now);
            db.CarrierAccounts.Add(account);
            await secrets.PutAsync(secretReference, request.Username, request.Password, account.ConfigurationVersion, cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(MapAccount(account));
    }

    public async Task<Result<ShippingCarrierConnectionTestOutcome>> TestConnectionAsync(
        ShippingAuthoringOwnerScope owner,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await FindOwnedAsync(owner, accountId, cancellationToken);
        if (account is null)
        {
            return Result.Failure<ShippingCarrierConnectionTestOutcome>(
                new SemanticError(ShippingErrorCodes.CarrierAccountNotFound));
        }

        if (!account.IsActive)
        {
            account.RecordConnectionResult(ShippingCarrierConnectionTestOutcome.Disabled, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success(ShippingCarrierConnectionTestOutcome.Disabled);
        }

        var secret = await secrets.TryGetAsync(account.SecretReference, account.ConfigurationVersion, cancellationToken);
        if (secret is null)
        {
            account.RecordConnectionResult(ShippingCarrierConnectionTestOutcome.SecretMissing, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync(cancellationToken);
            return Result.Success(ShippingCarrierConnectionTestOutcome.SecretMissing);
        }

        var outcome = await tipax.TestConnectionAsync(account, secret, cancellationToken);
        account.RecordConnectionResult(outcome, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(outcome);
    }

    public async Task<Result<IReadOnlyList<ShippingCarrierReferenceValueView>>> SyncReferencesAsync(
        ShippingAuthoringOwnerScope owner,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await FindOwnedAsync(owner, accountId, cancellationToken);
        if (account is null)
        {
            return Result.Failure<IReadOnlyList<ShippingCarrierReferenceValueView>>(
                new SemanticError(ShippingErrorCodes.CarrierAccountNotFound));
        }

        if (!account.IsActive)
        {
            return Result.Failure<IReadOnlyList<ShippingCarrierReferenceValueView>>(
                new SemanticError(ShippingErrorCodes.CarrierBindingInvalid));
        }

        var secret = await secrets.TryGetAsync(account.SecretReference, account.ConfigurationVersion, cancellationToken);
        if (secret is null)
        {
            return Result.Failure<IReadOnlyList<ShippingCarrierReferenceValueView>>(
                new SemanticError(ShippingErrorCodes.CarrierBindingInvalid));
        }

        var synced = await tipax.SyncReferencesAsync(account, secret, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var existing = await db.CarrierReferenceValues.Where(x => x.AccountId == account.AccountId).ToListAsync(cancellationToken);
        db.CarrierReferenceValues.RemoveRange(existing);
        foreach (var item in synced)
        {
            db.CarrierReferenceValues.Add(item);
        }

        account.RecordReferenceSync(now);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ShippingCarrierReferenceValueView>>(
            synced.Select(MapReference).ToList());
    }

    public async Task<Result<IReadOnlyList<ShippingCarrierReferenceValueView>>> ListReferencesAsync(
        ShippingAuthoringOwnerScope owner,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (await FindOwnedAsync(owner, accountId, cancellationToken) is null)
        {
            return Result.Failure<IReadOnlyList<ShippingCarrierReferenceValueView>>(
                new SemanticError(ShippingErrorCodes.CarrierAccountNotFound));
        }

        var rows = await db.CarrierReferenceValues.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .OrderBy(x => x.Kind).ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ShippingCarrierReferenceValueView>>(rows.Select(MapReference).ToList());
    }

    public async Task<Result<IReadOnlyList<ShippingCarrierDestinationMappingView>>> ListDestinationMappingsAsync(
        ShippingAuthoringOwnerScope owner,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (await FindOwnedAsync(owner, accountId, cancellationToken) is null)
        {
            return Result.Failure<IReadOnlyList<ShippingCarrierDestinationMappingView>>(
                new SemanticError(ShippingErrorCodes.CarrierAccountNotFound));
        }

        var rows = await db.CarrierDestinationMappings.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .OrderBy(x => x.ExternalLocationCode)
            .ToListAsync(cancellationToken);
        return Result.Success<IReadOnlyList<ShippingCarrierDestinationMappingView>>(rows.Select(MapMapping).ToList());
    }

    public async Task<Result<ShippingCarrierDestinationMappingView>> UpsertDestinationMappingAsync(
        ShippingAuthoringOwnerScope owner,
        Guid accountId,
        ShippingCarrierDestinationMappingUpsertRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var account = await FindOwnedAsync(owner, accountId, cancellationToken);
        if (account is null)
        {
            return Result.Failure<ShippingCarrierDestinationMappingView>(
                new SemanticError(ShippingErrorCodes.CarrierAccountNotFound));
        }

        var now = DateTimeOffset.UtcNow;
        var existing = await db.CarrierDestinationMappings
            .SingleOrDefaultAsync(
                x => x.AccountId == accountId && x.GeographyCityId == request.GeographyCityId,
                cancellationToken);
        if (existing is null)
        {
            existing = ShippingCarrierDestinationMapping.Create(
                Guid.NewGuid(),
                accountId,
                account.ProviderCode,
                request.GeographyCityId,
                request.ExternalLocationCode,
                request.Provenance,
                now);
            db.CarrierDestinationMappings.Add(existing);
        }
        else
        {
            existing.Replace(request.ExternalLocationCode, request.Provenance, now);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(MapMapping(existing));
    }

    public async Task<Result<ShippingCarrierMethodBindingView?>> GetMethodBindingAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        CancellationToken cancellationToken)
    {
        var key = ToDomainOwner(owner);
        var draftOwned = await db.AuthoringDrafts.AsNoTracking().AnyAsync(
            x => x.DraftId == draftId
                 && x.OwnerKind == key.Kind
                 && x.OwnerTenantId == key.TenantId
                 && x.OwnerSellerPartyId == key.SellerPartyId,
            cancellationToken);
        if (!draftOwned)
        {
            return Result.Failure<ShippingCarrierMethodBindingView?>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftNotFound));
        }

        var binding = await db.CarrierMethodBindings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DraftId == draftId, cancellationToken);
        return Result.Success(binding is null ? null : MapBinding(binding));
    }

    public async Task<Result<ShippingCarrierMethodBindingView>> ReplaceMethodBindingAsync(
        ShippingAuthoringOwnerScope owner,
        Guid draftId,
        ShippingCarrierMethodBindingReplaceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var key = ToDomainOwner(owner);
        var draft = await db.AuthoringDrafts.SingleOrDefaultAsync(
            x => x.DraftId == draftId
                 && x.OwnerKind == key.Kind
                 && x.OwnerTenantId == key.TenantId
                 && x.OwnerSellerPartyId == key.SellerPartyId,
            cancellationToken);
        if (draft is null)
        {
            return Result.Failure<ShippingCarrierMethodBindingView>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftNotFound));
        }

        if (draft.Revision != request.ExpectedRevision)
        {
            return Result.Failure<ShippingCarrierMethodBindingView>(
                new SemanticError(ShippingErrorCodes.AuthoringDraftRevisionConflict));
        }

        var account = await FindOwnedAsync(owner, request.CarrierAccountId, cancellationToken);
        if (account is null || !account.IsActive)
        {
            return Result.Failure<ShippingCarrierMethodBindingView>(
                new SemanticError(ShippingErrorCodes.CarrierBindingInvalid));
        }

        var now = DateTimeOffset.UtcNow;
        var existing = await db.CarrierMethodBindings.SingleOrDefaultAsync(x => x.DraftId == draftId, cancellationToken);
        if (existing is null)
        {
            existing = ShippingCarrierMethodBinding.Create(
                Guid.NewGuid(),
                draftId,
                account.AccountId,
                account.ProviderCode,
                request.ProviderServiceId,
                request.PaymentTypeId,
                request.PickupTypeId,
                request.DistributionTypeId,
                request.PackingId,
                request.PackageContentId,
                request.PackTypeId,
                request.OriginExternalCityCode,
                request.OriginAddressLine,
                request.OriginLatitude,
                request.OriginLongitude,
                request.DefaultLengthCentimeters,
                request.DefaultWidthCentimeters,
                request.DefaultHeightCentimeters,
                account.ConfigurationVersion,
                now);
            db.CarrierMethodBindings.Add(existing);
        }
        else
        {
            existing.Replace(
                account.AccountId,
                account.ProviderCode,
                request.ProviderServiceId,
                request.PaymentTypeId,
                request.PickupTypeId,
                request.DistributionTypeId,
                request.PackingId,
                request.PackageContentId,
                request.PackTypeId,
                request.OriginExternalCityCode,
                request.OriginAddressLine,
                request.OriginLatitude,
                request.OriginLongitude,
                request.DefaultLengthCentimeters,
                request.DefaultWidthCentimeters,
                request.DefaultHeightCentimeters,
                account.ConfigurationVersion,
                now);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success(MapBinding(existing));
    }

    public async Task<ShippingCarrierAccountExecutionContext?> TryResolveExecutionContextAsync(
        ShippingDraftOwnerKind ownerKind,
        string? ownerTenantId,
        Guid? ownerSellerPartyId,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var account = await db.CarrierAccounts.AsNoTracking().SingleOrDefaultAsync(
            x => x.AccountId == accountId
                 && x.OwnerKind == ownerKind
                 && x.OwnerTenantId == ownerTenantId
                 && x.OwnerSellerPartyId == ownerSellerPartyId,
            cancellationToken);
        if (account is null)
        {
            return null;
        }

        var secret = await secrets.TryGetAsync(account.SecretReference, account.ConfigurationVersion, cancellationToken);
        if (secret is null)
        {
            return null;
        }

        var binding = await db.CarrierMethodBindings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CarrierAccountId == accountId, cancellationToken);

        return new ShippingCarrierAccountExecutionContext(
            account.AccountId,
            account.ProviderCode,
            account.DisplayName,
            account.IsActive,
            account.ConfigurationVersion,
            account.ApiBaseUrl,
            account.TokenUrl,
            account.CurrencyCode,
            secret.Username,
            secret.Password,
            binding is null ? null : MapBinding(binding));
    }

    public async Task<string?> TryResolveDestinationExternalCodeAsync(
        Guid accountId,
        Guid geographyCityId,
        CancellationToken cancellationToken)
    {
        var mapping = await db.CarrierDestinationMappings.AsNoTracking().SingleOrDefaultAsync(
            x => x.AccountId == accountId && x.GeographyCityId == geographyCityId,
            cancellationToken);
        return mapping?.ExternalLocationCode;
    }

    public async Task<CarrierBindingExecution?> GetMethodBindingForDraftAsync(
        Guid draftId,
        ShippingDraftOwnerKind ownerKind,
        string? ownerTenantId,
        Guid? ownerSellerPartyId,
        CancellationToken cancellationToken)
    {
        var binding = await db.CarrierMethodBindings.AsNoTracking()
            .SingleOrDefaultAsync(x => x.DraftId == draftId, cancellationToken);
        if (binding is null)
        {
            return null;
        }

        var account = await db.CarrierAccounts.AsNoTracking().SingleOrDefaultAsync(
            x => x.AccountId == binding.CarrierAccountId
                 && x.OwnerKind == ownerKind
                 && x.OwnerTenantId == ownerTenantId
                 && x.OwnerSellerPartyId == ownerSellerPartyId,
            cancellationToken);
        if (account is null)
        {
            return null;
        }

        var secret = await secrets.TryGetAsync(account.SecretReference, account.ConfigurationVersion, cancellationToken);
        if (secret is null)
        {
            return new CarrierBindingExecution(
                binding.BindingId,
                account.AccountId,
                account.ProviderCode,
                binding.ProviderServiceId,
                binding.OriginExternalCityCode,
                binding.DefaultLengthCentimeters,
                binding.DefaultWidthCentimeters,
                binding.DefaultHeightCentimeters,
                IsActiveAccount: false,
                Execution: new CarrierAccountExecution(
                    account.AccountId,
                    account.ConfigurationVersion,
                    Username: string.Empty,
                    Password: string.Empty,
                    account.CurrencyCode,
                    binding.PaymentTypeId,
                    binding.PickupTypeId,
                    binding.DistributionTypeId,
                    binding.PackingId,
                    binding.PackageContentId,
                    binding.PackTypeId,
                    account.ApiBaseUrl,
                    account.TokenUrl));
        }

        return new CarrierBindingExecution(
            binding.BindingId,
            account.AccountId,
            account.ProviderCode,
            binding.ProviderServiceId,
            binding.OriginExternalCityCode,
            binding.DefaultLengthCentimeters,
            binding.DefaultWidthCentimeters,
            binding.DefaultHeightCentimeters,
            account.IsActive,
            new CarrierAccountExecution(
                account.AccountId,
                account.ConfigurationVersion,
                secret.Username,
                secret.Password,
                account.CurrencyCode,
                binding.PaymentTypeId,
                binding.PickupTypeId,
                binding.DistributionTypeId,
                binding.PackingId,
                binding.PackageContentId,
                binding.PackTypeId,
                account.ApiBaseUrl,
                account.TokenUrl));
    }

    private async Task<ShippingCarrierAccount?> FindOwnedAsync(
        ShippingAuthoringOwnerScope owner,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var key = ToDomainOwner(owner);
        return await OwnedAccounts(key).SingleOrDefaultAsync(x => x.AccountId == accountId, cancellationToken);
    }

    private IQueryable<ShippingCarrierAccount> OwnedAccounts(ShippingDraftOwner key)
        => db.CarrierAccounts.Where(x =>
            x.OwnerKind == key.Kind
            && x.OwnerTenantId == key.TenantId
            && x.OwnerSellerPartyId == key.SellerPartyId);

    private static ShippingDraftOwner ToDomainOwner(ShippingAuthoringOwnerScope owner) => owner.Kind switch
    {
        ShippingAuthoringOwnerKind.Store when owner.TenantId is { } tenant =>
            ShippingDraftOwner.ForStore(tenant),
        ShippingAuthoringOwnerKind.Marketplace when owner.TenantId is null && owner.SellerPartyId is null =>
            ShippingDraftOwner.ForMarketplace(),
        ShippingAuthoringOwnerKind.Seller when owner.TenantId is null && owner.SellerPartyId is { } seller =>
            ShippingDraftOwner.ForSeller(seller),
        _ => throw new SemanticException(new SemanticError(ShippingErrorCodes.AuthoringOwnerScopeUnavailable)),
    };

    private static ShippingCarrierAccountView MapAccount(ShippingCarrierAccount account)
        => new(
            account.AccountId,
            account.ProviderCode,
            account.DisplayName,
            account.Environment,
            account.SecretReference,
            account.IsActive,
            account.ConfigurationVersion,
            account.LastConnectionOutcome,
            account.LastConnectionAt,
            account.LastReferenceSyncAt);

    private static ShippingCarrierReferenceValueView MapReference(ShippingCarrierReferenceValue value)
        => new(value.Kind, value.ExternalId, value.Title, value.IsActive);

    private static ShippingCarrierDestinationMappingView MapMapping(ShippingCarrierDestinationMapping mapping)
        => new(
            mapping.MappingId,
            mapping.AccountId,
            mapping.ProviderCode,
            mapping.GeographyCityId,
            mapping.ExternalLocationCode,
            mapping.Provenance,
            mapping.UpdatedAt);

    private static ShippingCarrierMethodBindingView MapBinding(ShippingCarrierMethodBinding binding)
        => new(
            binding.BindingId,
            binding.DraftId,
            binding.CarrierAccountId,
            binding.ProviderCode,
            binding.ProviderServiceId,
            binding.PaymentTypeId,
            binding.PickupTypeId,
            binding.DistributionTypeId,
            binding.PackingId,
            binding.PackageContentId,
            binding.PackTypeId,
            binding.OriginExternalCityCode,
            binding.OriginAddressLine,
            binding.OriginLatitude,
            binding.OriginLongitude,
            binding.DefaultLengthCentimeters,
            binding.DefaultWidthCentimeters,
            binding.DefaultHeightCentimeters,
            binding.ConfigurationVersion,
            binding.UpdatedAt);
}
