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
            .ThenBy(x => x.OptionCode)
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
        var optionCode = NormalizeOption(request.OptionCode);

        if (await RateScopeExistsAsync(
                context.RatePolicies,
                code,
                optionCode,
                scope,
                request.Condition?.ToCondition(),
                cancellationToken))
        {
            return Result.Failure<ShippingRatePolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.RatePolicyScopeDuplicate));
        }

        var now = DateTimeOffset.UtcNow;
        ShippingRatePolicy policy;
        try
        {
            policy = ShippingRatePolicy.Create(
                code,
                scope,
                request.ChargeAmount,
                request.CurrencyCode,
                request.PreparationDays,
                request.LeadDays,
                request.FreeAboveThreshold,
                now,
                ratePolicyId: null,
                optionCode: optionCode,
                condition: request.Condition?.ToCondition());

            // A second active tier that intersects the new one on the same identity+scope would make
            // "which tier prices this parcel" depend on row order. The pair is refused before any row
            // is written, and an invalid tier shape is refused here too.
            if (await TierOverlapsExistingAsync(
                    context.RatePolicies,
                    code,
                    optionCode,
                    scope,
                    request.Condition?.ToCondition(),
                    cancellationToken))
            {
                return Result.Failure<ShippingRatePolicyAdminItem>(
                    new SemanticError(ShippingErrorCodes.RatePolicyTierOverlap));
            }
        }
        catch (SemanticException ex)
        {
            return Result.Failure<ShippingRatePolicyAdminItem>(ex.Error);
        }

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

        // The option identity of an existing policy is immutable for the same reason: re-pointing a
        // policy from one option to another would move a commercial decision between methods.
        if (!SameOption(policy.OptionCode, NormalizeOption(request.OptionCode)))
        {
            return Result.Failure<ShippingRatePolicyAdminItem>(
                new SemanticError(ShippingErrorCodes.RatePolicyOptionMismatch));
        }

        var now = DateTimeOffset.UtcNow;
        try
        {
            // The measurement tier is an editable attribute of the same commercial rule, so it is
            // replaced together with the amount/lead times; identity and scope stay immutable above.
            var condition = request.Condition?.ToCondition();
            if (await TierOverlapsExistingAsync(
                    context.RatePolicies,
                    policy.ServiceCode,
                    policy.OptionCode,
                    policy.Scope,
                    condition,
                    cancellationToken,
                    excludedRatePolicyId: policy.RatePolicyId))
            {
                return Result.Failure<ShippingRatePolicyAdminItem>(
                    new SemanticError(ShippingErrorCodes.RatePolicyTierOverlap));
            }

            policy.ChangeCharge(request.ChargeAmount, now);
            policy.SetFreeAboveThreshold(request.FreeAboveThreshold, now);
            policy.ChangeLeadTimes(request.PreparationDays, request.LeadDays, now);
            policy.ChangeCondition(condition, now);
        }
        catch (SemanticException ex)
        {
            return Result.Failure<ShippingRatePolicyAdminItem>(ex.Error);
        }

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

        if (await GrantScopeExistsAsync(context.CoverageGrants, code, scope, cancellationToken))
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

    /// <summary>
    /// آیا همان هویت و دامنه و <em>همان پلهٔ اندازه</em> از قبل وجود دارد. یکتایی «هویت + دامنه +
    /// پله» است، نه فقط «هویت + دامنه»: دو پلهٔ وزنی مجاز و متمایز برای یک خدمت کاملاً معتبرند و
    /// تنها تکرار عین همان پله یک ردیف تکراری است. هم‌پوشانیِ ناقص (نه عین هم) در نگهبان جداگانه
    /// <c>TierOverlapsExistingAsync</c> رد می‌شود.
    /// </summary>
    private static async Task<bool> RateScopeExistsAsync(
        IQueryable<ShippingRatePolicy> set,
        string serviceCode,
        string? optionCode,
        Contracts.Rates.ShippingDestinationScope scope,
        Contracts.Rates.ShippingRateCondition? condition,
        CancellationToken cancellationToken)
    {
        // Comparison is done on the narrow per-service slice rather than with a provider-specific NULL
        // comparison expression, so the same guard behaves identically on PostgreSQL and in focused tests.
        var existing = await set
            .Where(x => x.ServiceCode == serviceCode)
            .ToListAsync(cancellationToken);

        return existing.Any(x =>
            SameOption(x.OptionCode, optionCode)
            && SameScope(x.Scope, scope)
            && SameTier(x.Condition, condition));
    }

    /// <summary>
    /// آیا دو شرط اندازه دقیقاً یک پله‌اند. «بی‌شرط» تنها یک نمایش دارد (هر دو محور تهی)، پس یک
    /// ردیف بی‌شرط تکراری هم اینجا رد می‌شود.
    /// </summary>
    private static bool SameTier(
        Contracts.Rates.ShippingRateCondition left,
        Contracts.Rates.ShippingRateCondition? right)
    {
        var normalizedRight = right is null || !right.IsConditioned
            ? Contracts.Rates.ShippingRateCondition.Unconditioned
            : right;

        return SameTierAxis(left.Weight, normalizedRight.Weight)
            && SameTierAxis(left.Count, normalizedRight.Count);
    }

    private static bool SameTierAxis(
        Contracts.Rates.ShippingNumericTier? left,
        Contracts.Rates.ShippingNumericTier? right)
        => (left, right) switch
        {
            (null, null) => true,
            (null, not null) or (not null, null) => false,
            _ => left!.Min == right!.Min && left.Max == right.Max,
        };

    private static async Task<bool> GrantScopeExistsAsync(
        IQueryable<ShippingCoverageGrant> set,
        string serviceCode,
        Contracts.Rates.ShippingDestinationScope scope,
        CancellationToken cancellationToken)
    {
        var existing = await set
            .Where(x => x.ServiceCode == serviceCode)
            .Select(x => new
            {
                x.CountryId,
                x.AdministrativeAreaId,
                x.CityId,
                x.ServiceAreaId,
            })
            .ToListAsync(cancellationToken);

        return existing.Any(x =>
            x.CountryId == scope.CountryId
            && x.AdministrativeAreaId == scope.AdministrativeAreaId
            && x.CityId == scope.CityId
            && x.ServiceAreaId == scope.ServiceAreaId);
    }

    /// <summary>
    /// آیا شرط اندازهٔ پیشنهادی با شرط یک سیاست فعال دیگر روی همان هویت (خدمت/گزینه/دامنه)
    /// هم‌پوشانی دارد. ردیف‌های هم‌هویت با هم رقابت می‌کنند، پس فقط همان‌ها بررسی می‌شوند.
    /// </summary>
    /// <remarks>
    /// بررسی در حافظه روی برش باریک همان خدمت انجام می‌شود (نه با عبارت provider-specific)، تا
    /// همان نگهبان روی PostgreSQL و در تست‌های متمرکز یکسان رفتار کند. طرف بی‌شرط هم‌پوشانی شمرده
    /// نمی‌شود، چون در موتور به‌عنوان قاعدهٔ پایهٔ مستند پذیرفته شده است.
    /// </remarks>
    private static async Task<bool> TierOverlapsExistingAsync(
        IQueryable<ShippingRatePolicy> set,
        string serviceCode,
        string? optionCode,
        Contracts.Rates.ShippingDestinationScope scope,
        Contracts.Rates.ShippingRateCondition? candidate,
        CancellationToken cancellationToken,
        Guid? excludedRatePolicyId = null)
    {
        if (candidate is null || !candidate.IsConditioned)
        {
            return false;
        }

        var siblings = await set
            .Where(x => x.ServiceCode == serviceCode)
            .Where(x => excludedRatePolicyId == null || x.RatePolicyId != excludedRatePolicyId)
            .ToListAsync(cancellationToken);

        return siblings
            .Where(x => x.IsActive)
            .Where(x => SameOption(x.OptionCode, optionCode))
            .Where(x => SameScope(x.Scope, scope))
            .Any(x => x.TiersOverlapWith(candidate));
    }

    private static bool SameScope(Contracts.Rates.ShippingDestinationScope left, Contracts.Rates.ShippingDestinationScope right) =>
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
            policy.UpdatedAt,
            policy.OptionCode,
            MapCondition(policy));

    /// <summary>شرط اندازهٔ سیاست را به شکل قرارداد ادمین می‌نگارد؛ بی‌شرط به <c>null</c> نگاشت می‌شود.</summary>
    private static Contracts.Admin.ShippingRateConditionWrite? MapCondition(ShippingRatePolicy policy)
        => policy.IsUnconditioned
            ? null
            : new Contracts.Admin.ShippingRateConditionWrite(
                policy.MinWeightGrams is { } minWeight
                    ? new Contracts.Admin.ShippingNumericTierWrite(minWeight, policy.MaxWeightGrams)
                    : null,
                policy.MinItemCount is { } minCount
                    ? new Contracts.Admin.ShippingNumericTierWrite(minCount, policy.MaxItemCount)
                    : null);

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
