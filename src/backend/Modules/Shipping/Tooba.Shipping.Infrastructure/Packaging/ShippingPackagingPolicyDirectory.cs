using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Shipping.Application.Packaging.Ports;
using Tooba.Shipping.Contracts.Errors;
using Tooba.Shipping.Contracts.Packaging;
using Tooba.Shipping.Contracts.Rates;
using Tooba.Shipping.Domain.Aggregates;
using Tooba.Shipping.Infrastructure.Persistence;

namespace Tooba.Shipping.Infrastructure.Packaging;

/// <summary>
/// پیاده‌سازی نویسندگی سیاست هزینهٔ بسته‌بندی روی <see cref="ShippingDbContext"/> خودِ همین ماژول.
/// هیچ DbContext ماژول دیگری خوانده یا نوشته نمی‌شود؛ کد خدمت فقط یک کلید مات Contracts-سازگار است.
/// </summary>
/// <remarks>
/// یکتایی تجاری از دو لایه محافظت می‌شود: بررسی پیش‌از‌نوشت اینجا (تا یک خطای کسب‌وکار روشن
/// برگردد) و ایندکس یکتای <c>NULLS NOT DISTINCT</c> در دیتابیس (تا هیچ نویسندهٔ دیگری نتواند
/// ردیف هم‌دامنه ثبت کند). هیچ‌کدام جای دیگری را نمی‌گیرد.
/// </remarks>
public sealed class ShippingPackagingPolicyDirectory(ShippingDbContext context) : IShippingPackagingPolicyDirectory
{
    /// <inheritdoc />
    public async Task<Result<ShippingPackagingPoliciesView>> ListAsync(
        string serviceCode,
        CancellationToken cancellationToken)
    {
        var code = Normalize(serviceCode);
        if (code.Length == 0)
        {
            return Result.Failure<ShippingPackagingPoliciesView>(
                new SemanticError(ShippingErrorCodes.ServiceCodeRequired));
        }

        var policies = await context.PackagingPolicies
            .AsNoTracking()
            .Where(x => x.ServiceCode == code)
            .OrderBy(x => x.CountryId)
            .ThenBy(x => x.AdministrativeAreaId)
            .ThenBy(x => x.CityId)
            .ThenBy(x => x.ServiceAreaId)
            .ThenBy(x => x.OptionCode)
            .Select(x => Map(x))
            .ToListAsync(cancellationToken);

        // An empty set is a real answer: it means ZERO packaging fee, never a hidden extra charge.
        return Result.Success(new ShippingPackagingPoliciesView(code, policies));
    }

    /// <inheritdoc />
    public async Task<Result<ShippingPackagingPolicyAdminItem>> CreateAsync(
        string serviceCode,
        ShippingPackagingPolicyWriteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var code = Normalize(serviceCode);
        if (code.Length == 0)
        {
            return Result.Failure<ShippingPackagingPolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.ServiceCodeRequired));
        }

        var scope = request.Scope.ToScope();
        var optionCode = NormalizeOption(request.OptionCode);

        if (await ScopeExistsAsync(context.PackagingPolicies, code, optionCode, scope, cancellationToken))
        {
            return Result.Failure<ShippingPackagingPolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.PackagingPolicyScopeDuplicate));
        }

        var now = DateTimeOffset.UtcNow;
        ShippingPackagingPolicy policy;
        try
        {
            policy = ShippingPackagingPolicy.Create(
                code,
                scope,
                request.FeeAmount,
                request.CurrencyCode,
                request.FreightPayerMode,
                now,
                packagingPolicyId: null,
                optionCode: optionCode);
        }
        catch (SemanticException ex)
        {
            return Result.Failure<ShippingPackagingPolicyAdminItem>(ex.Error);
        }

        context.PackagingPolicies.Add(policy);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(policy));
    }

    /// <inheritdoc />
    public async Task<Result<ShippingPackagingPolicyAdminItem>> UpdateAsync(
        Guid packagingPolicyId,
        string serviceCode,
        ShippingPackagingPolicyWriteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var policy = await context.PackagingPolicies
            .FirstOrDefaultAsync(x => x.PackagingPolicyId == packagingPolicyId, cancellationToken);
        if (policy is null)
        {
            return Result.Failure<ShippingPackagingPolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.PackagingPolicyNotFound));
        }

        // Cross-service mutation guard: the route service code is authoritative, so an id that belongs to
        // a different service is refused instead of being edited through another service's path.
        if (!BelongsTo(policy.ServiceCode, serviceCode))
        {
            return Result.Failure<ShippingPackagingPolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.ServiceScopeMismatch));
        }

        // The destination scope of an existing policy is immutable through this seam: changing it would
        // silently re-point an existing commercial decision, so a mismatch is refused rather than applied.
        if (!SameScope(policy.Scope, request.Scope.ToScope()))
        {
            return Result.Failure<ShippingPackagingPolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.CoverageScopeInvalid));
        }

        // The option identity is immutable for the same reason: re-pointing a policy from one option to
        // another would move a commercial decision between methods.
        if (!SameOption(policy.OptionCode, NormalizeOption(request.OptionCode)))
        {
            return Result.Failure<ShippingPackagingPolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.PackagingPolicyOptionMismatch));
        }

        var now = DateTimeOffset.UtcNow;
        try
        {
            policy.ChangeFee(request.FeeAmount, now);
            policy.ChangeCurrency(request.CurrencyCode, now);
            policy.ChangeFreightPayerMode(request.FreightPayerMode, now);
        }
        catch (SemanticException ex)
        {
            return Result.Failure<ShippingPackagingPolicyAdminItem>(ex.Error);
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(policy));
    }

    /// <inheritdoc />
    public async Task<Result<ShippingPackagingPolicyAdminItem>> SetActiveAsync(
        Guid packagingPolicyId,
        string serviceCode,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var policy = await context.PackagingPolicies
            .FirstOrDefaultAsync(x => x.PackagingPolicyId == packagingPolicyId, cancellationToken);
        if (policy is null)
        {
            return Result.Failure<ShippingPackagingPolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.PackagingPolicyNotFound));
        }

        if (!BelongsTo(policy.ServiceCode, serviceCode))
        {
            return Result.Failure<ShippingPackagingPolicyAdminItem>(
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

    private static bool BelongsTo(string storedServiceCode, string? routeServiceCode) =>
        string.Equals(
            storedServiceCode?.Trim() ?? string.Empty,
            routeServiceCode?.Trim() ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// آیا همان هویت (خدمت + گزینه) و همان دامنه از قبل وجود دارد. یکتایی «هویت + دامنه» است؛
    /// مقایسه روی برش باریک همان خدمت انجام می‌شود تا همان نگهبان روی PostgreSQL و در تست‌های
    /// متمرکز یکسان رفتار کند.
    /// </summary>
    private static async Task<bool> ScopeExistsAsync(
        IQueryable<ShippingPackagingPolicy> set,
        string serviceCode,
        string? optionCode,
        ShippingDestinationScope scope,
        CancellationToken cancellationToken)
    {
        var existing = await set
            .Where(x => x.ServiceCode == serviceCode)
            .Select(x => new
            {
                x.OptionCode,
                x.CountryId,
                x.AdministrativeAreaId,
                x.CityId,
                x.ServiceAreaId,
            })
            .ToListAsync(cancellationToken);

        return existing.Any(x =>
            SameOption(x.OptionCode, optionCode)
            && x.CountryId == scope.CountryId
            && x.AdministrativeAreaId == scope.AdministrativeAreaId
            && x.CityId == scope.CityId
            && x.ServiceAreaId == scope.ServiceAreaId);
    }

    private static bool SameScope(ShippingDestinationScope left, ShippingDestinationScope right) =>
        left.CountryId == right.CountryId
        && left.AdministrativeAreaId == right.AdministrativeAreaId
        && left.CityId == right.CityId
        && left.ServiceAreaId == right.ServiceAreaId;

    private static bool SameOption(string? left, string? right) =>
        string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string? serviceCode) => serviceCode?.Trim() ?? string.Empty;

    /// <summary>کد گزینه را به شکل متعارف می‌برد؛ تهی/فاصله یعنی سیاست در سطح خدمت است.</summary>
    private static string? NormalizeOption(string? optionCode)
    {
        var trimmed = optionCode?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static ShippingPackagingPolicyAdminItem Map(ShippingPackagingPolicy policy) =>
        new(
            policy.PackagingPolicyId,
            policy.ServiceCode,
            policy.CountryId,
            policy.AdministrativeAreaId,
            policy.CityId,
            policy.ServiceAreaId,
            policy.Scope.Level,
            policy.FeeAmount,
            policy.CurrencyCode,
            policy.FreightPayerMode,
            policy.IsActive,
            policy.CreatedAt,
            policy.UpdatedAt,
            policy.OptionCode);
}
