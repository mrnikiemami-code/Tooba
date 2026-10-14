using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Rates.Ports;
using Tooba.Shipping.Contracts.Admin;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Infrastructure.Persistence;

namespace Tooba.Shipping.Infrastructure.Rates;

/// <summary>
/// پیاده‌سازی نویسندگی نرخ/پوشش روی <see cref="ShippingDbContext"/> خودِ همین ماژول. هیچ
/// DbContext ماژول دیگری خوانده یا نوشته نمی‌شود؛ کد خدمت فقط یک کلید مات Contracts-سازگار است.
/// </summary>
/// <remarks>
/// یکتایی تجاری از دو لایه محافظت می‌شود: بررسی پیش‌از‌نوشت اینجا (تا یک خطای کسب‌وکار روشن
/// برگردد) و ایندکس یکتای <c>NULLS NOT DISTINCT</c> در دیتابیس (تا هیچ نویسندهٔ دیگری نتواند
/// ردیف هم‌دامنه ثبت کند). هیچ‌کدام جای دیگری را نمی‌گیرد.
/// </remarks>
public sealed class ShippingRateCoverageDirectory(ShippingDbContext context) : IShippingRateCoverageDirectory
{
    /// <inheritdoc />
    public async Task<Result<ShippingRatesAndCoverageView>> ListAsync(
        string serviceCode,
        CancellationToken cancellationToken)
    {
        var code = Normalize(serviceCode);
        if (code.Length == 0)
        {
            return Result.Failure<ShippingRatesAndCoverageView>(
                new SemanticError(ShippingErrorCodes.ServiceCodeRequired));
        }

        var rates = await context.RatePolicies
            .AsNoTracking()
            .Where(x => x.ServiceCode == code)
            .OrderBy(x => x.CountryId)
            .ThenBy(x => x.AdministrativeAreaId)
            .ThenBy(x => x.CityId)
            .ThenBy(x => x.ServiceAreaId)
            .Select(x => Map(x))
            .ToListAsync(cancellationToken);

        var grants = await context.CoverageGrants
            .AsNoTracking()
            .Where(x => x.ServiceCode == code)
            .OrderBy(x => x.CountryId)
            .ThenBy(x => x.AdministrativeAreaId)
            .ThenBy(x => x.CityId)
            .ThenBy(x => x.ServiceAreaId)
            .Select(x => Map(x))
            .ToListAsync(cancellationToken);

        // An empty coverage set is a real answer: it means NO COVERAGE, never "unrestricted".
        return Result.Success(new ShippingRatesAndCoverageView(code, rates, grants));
    }

    /// <inheritdoc />
    public async Task<Result<ShippingRatePolicyAdminItem>> CreateRateAsync(
        string serviceCode,
        ShippingRatePolicyWriteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var code = Normalize(serviceCode);
        if (code.Length == 0)
        {
            return Result.Failure<ShippingRatePolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.ServiceCodeRequired));
        }

        var scope = request.Scope.ToScope();

        if (await ScopeExistsAsync(context.RatePolicies, code, scope, cancellationToken))
        {
            return Result.Failure<ShippingRatePolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.RatePolicyScopeDuplicate));
        }

        var now = DateTimeOffset.UtcNow;
        var policy = ShippingRatePolicy.Create(
            code,
            scope,
            request.ChargeAmount,
            request.CurrencyCode,
            request.PreparationDays,
            request.LeadDays,
            request.FreeAboveThreshold,
            now);

        context.RatePolicies.Add(policy);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(policy));
    }

    /// <inheritdoc />
    public async Task<Result<ShippingRatePolicyAdminItem>> UpdateRateAsync(
        Guid ratePolicyId,
        string serviceCode,
        ShippingRatePolicyWriteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var policy = await context.RatePolicies
            .FirstOrDefaultAsync(x => x.RatePolicyId == ratePolicyId, cancellationToken);
        if (policy is null)
        {
            return Result.Failure<ShippingRatePolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.RatePolicyNotFound));
        }

        // Cross-service mutation guard: the route service code is authoritative, so an id that belongs to
        // a different service is refused instead of being edited through another service's path.
        if (!BelongsTo(policy.ServiceCode, serviceCode))
        {
            return Result.Failure<ShippingRatePolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.ServiceScopeMismatch));
        }

        // The destination scope of an existing policy is immutable through this seam: changing it would
        // silently re-point an existing commercial decision, so a mismatch is refused rather than applied.
        if (!SameScope(policy.Scope, request.Scope.ToScope()))
        {
            return Result.Failure<ShippingRatePolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.CoverageScopeInvalid));
        }

        var now = DateTimeOffset.UtcNow;
        policy.ChangeCharge(request.ChargeAmount, now);
        policy.SetFreeAboveThreshold(request.FreeAboveThreshold, now);
        policy.ChangeLeadTimes(request.PreparationDays, request.LeadDays, now);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(policy));
    }

    /// <inheritdoc />
    public async Task<Result<ShippingRatePolicyAdminItem>> SetRateActiveAsync(
        Guid ratePolicyId,
        string serviceCode,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var policy = await context.RatePolicies
            .FirstOrDefaultAsync(x => x.RatePolicyId == ratePolicyId, cancellationToken);
        if (policy is null)
        {
            return Result.Failure<ShippingRatePolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.RatePolicyNotFound));
        }

        if (!BelongsTo(policy.ServiceCode, serviceCode))
        {
            return Result.Failure<ShippingRatePolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.ServiceScopeMismatch));
        }

        var now = DateTimeOffset.UtcNow;
        if (isActive)
        {
            policy.Activate(now);
        }
        else
        {
            policy.Deactivate(now);
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(policy));
    }

    /// <inheritdoc />
    public async Task<Result<ShippingCoverageGrantAdminItem>> CreateGrantAsync(
        string serviceCode,
        ShippingCoverageGrantWriteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var code = Normalize(serviceCode);
        if (code.Length == 0)
        {
            return Result.Failure<ShippingCoverageGrantAdminItem>(
                new SemanticError(ShippingErrorCodes.ServiceCodeRequired));
        }

        var scope = request.Scope.ToScope();

        if (await ScopeExistsAsync(context.CoverageGrants, code, scope, cancellationToken))
        {
            return Result.Failure<ShippingCoverageGrantAdminItem>(
                new SemanticError(ShippingErrorCodes.CoverageGrantScopeDuplicate));
        }

        var grant = ShippingCoverageGrant.Create(code, scope, DateTimeOffset.UtcNow);
        context.CoverageGrants.Add(grant);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(grant));
    }

    /// <inheritdoc />
    public async Task<Result<ShippingCoverageGrantAdminItem>> SetGrantActiveAsync(
        Guid coverageGrantId,
        string serviceCode,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var grant = await context.CoverageGrants
            .FirstOrDefaultAsync(x => x.CoverageGrantId == coverageGrantId, cancellationToken);
        if (grant is null)
        {
            return Result.Failure<ShippingCoverageGrantAdminItem>(
                new SemanticError(ShippingErrorCodes.CoverageGrantNotFound));
        }

        if (!BelongsTo(grant.ServiceCode, serviceCode))
        {
            return Result.Failure<ShippingCoverageGrantAdminItem>(
                new SemanticError(ShippingErrorCodes.ServiceScopeMismatch));
        }

        var now = DateTimeOffset.UtcNow;
        if (isActive)
        {
            grant.Activate(now);
        }
        else
        {
            grant.Deactivate(now);
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(grant));
    }

    private static bool BelongsTo(string storedServiceCode, string? routeServiceCode) =>
        string.Equals(
            storedServiceCode?.Trim() ?? string.Empty,
            routeServiceCode?.Trim() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);

    private static async Task<bool> ScopeExistsAsync<TEntity>(
        IQueryable<TEntity> set,
        string serviceCode,
        Contracts.Rates.ShippingDestinationScope scope,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        // Comparison is done on the narrow per-service slice rather than with a provider-specific NULL
        // comparison expression, so the same guard behaves identically on PostgreSQL and in focused tests.
        var existing = await set
            .Where(x => EF.Property<string>(x, nameof(ShippingRatePolicy.ServiceCode)) == serviceCode)
            .Select(x => new
            {
                CountryId = EF.Property<Guid>(x, nameof(ShippingRatePolicy.CountryId)),
                AdministrativeAreaId = EF.Property<Guid?>(x, nameof(ShippingRatePolicy.AdministrativeAreaId)),
                CityId = EF.Property<Guid?>(x, nameof(ShippingRatePolicy.CityId)),
                ServiceAreaId = EF.Property<Guid?>(x, nameof(ShippingRatePolicy.ServiceAreaId)),
            })
            .ToListAsync(cancellationToken);

        return existing.Any(x =>
            x.CountryId == scope.CountryId
            && x.AdministrativeAreaId == scope.AdministrativeAreaId
            && x.CityId == scope.CityId
            && x.ServiceAreaId == scope.ServiceAreaId);
    }

    private static bool SameScope(Contracts.Rates.ShippingDestinationScope left, Contracts.Rates.ShippingDestinationScope right) =>
        left.CountryId == right.CountryId
        && left.AdministrativeAreaId == right.AdministrativeAreaId
        && left.CityId == right.CityId
        && left.ServiceAreaId == right.ServiceAreaId;

    private static string Normalize(string? serviceCode) => serviceCode?.Trim() ?? string.Empty;

    private static ShippingRatePolicyAdminItem Map(ShippingRatePolicy policy) =>
        new(
            policy.RatePolicyId,
            policy.ServiceCode,
            policy.CountryId,
            policy.AdministrativeAreaId,
            policy.CityId,
            policy.ServiceAreaId,
            policy.Scope.Level,
            policy.ChargeAmount,
            policy.CurrencyCode,
            policy.PreparationDays,
            policy.LeadDays,
            policy.FreeAboveThreshold,
            policy.IsActive,
            policy.CreatedAt,
            policy.UpdatedAt);

    private static ShippingCoverageGrantAdminItem Map(ShippingCoverageGrant grant) =>
        new(
            grant.CoverageGrantId,
            grant.ServiceCode,
            grant.CountryId,
            grant.AdministrativeAreaId,
            grant.CityId,
            grant.ServiceAreaId,
            grant.Scope.Level,
            grant.IsActive,
            grant.CreatedAt,
            grant.UpdatedAt);
}
