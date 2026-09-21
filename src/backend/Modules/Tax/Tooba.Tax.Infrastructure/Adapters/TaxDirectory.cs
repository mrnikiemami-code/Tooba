using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;
using Tooba.Tax.Application;
using Tooba.Tax.Contracts;
using Tooba.Tax.Domain;
using Tooba.Tax.Infrastructure.Persistence;

namespace Tooba.Tax.Infrastructure;

/// <summary>Open Tax use-case guard.</summary>
public sealed class OpenTaxUseCaseGuard : ITaxUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>Tax configuration and calculation in the tax schema. Does not rewrite Pricing or Order.</summary>
public sealed class TaxDirectory : ITaxDirectory, ITaxQueryGateway
{
    private readonly TaxDbContext _db;
    private readonly ITaxUseCaseGuard _guard;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;

    /// <summary>Binds the directory to the tax schema.</summary>
    public TaxDirectory(TaxDbContext db, ITaxUseCaseGuard guard, IClock? clock = null, IIdGenerator? ids = null)
    {
        _db = db;
        _guard = guard;
        _clock = clock ?? new SystemUtcClock();
        _ids = ids ?? new UuidV7IdGenerator();
    }

    /// <inheritdoc />
    public async Task<TaxCategoryReference> CreateCategoryAsync(string code, string displayName, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var category = TaxCategory.Create(_ids.NewId(), code, displayName, _clock.UtcNow);
        _db.Categories.Add(category);
        await _db.SaveChangesAsync(cancellationToken);
        return new TaxCategoryReference(category.CategoryId, category.Code, category.DisplayName);
    }

    /// <inheritdoc />
    public async Task AssignOfferCategoryAsync(Guid offerId, Guid categoryId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            throw new InvalidOperationException("tax.category.missing");
        }

        var existing = await _db.OfferClassifications.SingleOrDefaultAsync(x => x.OfferId == offerId, cancellationToken);
        if (existing is not null)
        {
            _db.OfferClassifications.Remove(existing);
        }

        _db.OfferClassifications.Add(TaxOfferClassification.Assign(offerId, categoryId));
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TaxRuleReference> CreateRuleAsync(
        string jurisdiction,
        string market,
        Guid categoryId,
        TaxRuleKind kind,
        decimal rate,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveTo,
        int specificity,
        TaxOverridePolicy overridePolicy,
        CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        if (!await _db.Categories.AnyAsync(x => x.CategoryId == categoryId, cancellationToken))
        {
            throw new InvalidOperationException("tax.category.missing");
        }

        var rule = TaxRule.Create(
            _ids.NewId(),
            jurisdiction,
            market,
            categoryId,
            kind,
            rate,
            effectiveFrom,
            effectiveTo,
            specificity,
            overridePolicy,
            _clock.UtcNow);
        _db.Rules.Add(rule);
        await _db.SaveChangesAsync(cancellationToken);
        return ToRuleReference(rule);
    }

    /// <inheritdoc />
    public async Task ActivateRuleAsync(Guid ruleId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var rule = await _db.Rules.SingleAsync(x => x.RuleId == ruleId, cancellationToken);
        rule.Activate(_clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ChangeRuleRateAsync(Guid ruleId, decimal rate, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var rule = await _db.Rules.SingleAsync(x => x.RuleId == ruleId, cancellationToken);
        rule.ChangeRate(rate, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TaxCalculationResult> CalculateAsync(TaxCalculationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var at = request.At;
        var exclusive = request.TaxExclusiveAmount;
        if (string.IsNullOrWhiteSpace(request.Jurisdiction))
        {
            return Fail(request, TaxOutcome.CalculationError, exclusive);
        }

        if (string.IsNullOrWhiteSpace(request.Currency) || request.Quantity <= 0 || exclusive < 0)
        {
            return Fail(request, TaxOutcome.CalculationError, exclusive);
        }

        var classification = await _db.OfferClassifications.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OfferId == request.OfferId, cancellationToken);
        if (classification is null)
        {
            return Fail(request, TaxOutcome.NoApplicableRule, exclusive);
        }

        var candidates = await _db.Rules.AsNoTracking()
            .Where(x => x.Status == TaxRuleStatus.Active
                        && x.Jurisdiction == request.Jurisdiction.Trim()
                        && x.Market == request.Market.Trim()
                        && x.CategoryId == classification.CategoryId)
            .ToListAsync(cancellationToken);
        var effective = candidates.Where(x => x.IsEffectiveAt(at)).ToList();
        if (effective.Count == 0)
        {
            return Fail(request, TaxOutcome.NoApplicableRule, exclusive);
        }

        var maxSpecificity = effective.Max(x => x.Specificity);
        var winners = effective.Where(x => x.Specificity == maxSpecificity).ToList();
        if (winners.Count != 1)
        {
            return Fail(request, TaxOutcome.CalculationError, exclusive);
        }

        var rule = winners[0];
        var rate = rule.Rate;
        if (request.AllowTrustedOverride
            && request.TrustedOverrideRate is { } trusted
            && rule.OverridePolicy == TaxOverridePolicy.TrustedInternal)
        {
            if (trusted < 0 || trusted > 1)
            {
                return Fail(request, TaxOutcome.CalculationError, exclusive);
            }

            rate = trusted;
        }

        var lineExclusive = TaxRounding.Round(exclusive * request.Quantity, request.Currency, request.RoundingMode);
        decimal taxAmount;
        TaxOutcome outcome;
        switch (rule.Kind)
        {
            case TaxRuleKind.Exempt:
                taxAmount = 0m;
                outcome = TaxOutcome.Exempt;
                rate = 0m;
                break;
            case TaxRuleKind.ZeroRated:
                taxAmount = 0m;
                outcome = TaxOutcome.ZeroRated;
                rate = 0m;
                break;
            default:
                taxAmount = TaxRounding.Round(lineExclusive * rate, request.Currency, request.RoundingMode);
                outcome = TaxOutcome.Taxable;
                break;
        }

        return new TaxCalculationResult(
            outcome,
            lineExclusive,
            rate,
            taxAmount,
            lineExclusive + taxAmount,
            request.Currency.Trim().ToUpperInvariant(),
            rule.RuleId,
            classification.CategoryId,
            at);
    }

    private static TaxCalculationResult Fail(TaxCalculationRequest request, TaxOutcome outcome, decimal exclusive) =>
        new(
            outcome,
            exclusive,
            0m,
            0m,
            exclusive,
            request.Currency.Trim().ToUpperInvariant(),
            null,
            null,
            request.At);

    private static TaxRuleReference ToRuleReference(TaxRule rule) =>
        new(
            rule.RuleId,
            rule.Jurisdiction,
            rule.Market,
            rule.CategoryId,
            rule.Kind,
            rule.Rate,
            rule.EffectiveFrom,
            rule.EffectiveTo,
            rule.Status,
            rule.Specificity);

    /// <inheritdoc />
    public async Task<TaxCategorySnapshot?> FindCategoryByCodesAsync(
        IReadOnlyCollection<string> codes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(codes);
        if (codes.Count == 0)
        {
            return null;
        }

        var wanted = codes.Where(code => !string.IsNullOrWhiteSpace(code)).Select(code => code.Trim()).ToArray();
        var existing = await _db.Categories.AsNoTracking()
            .FirstOrDefaultAsync(category => wanted.Contains(category.Code), cancellationToken);
        return existing is null
            ? null
            : new TaxCategorySnapshot(existing.CategoryId, existing.Code, existing.DisplayName);
    }

    /// <inheritdoc />
    public Task<bool> HasActiveRuleAsync(
        Guid categoryId,
        string jurisdiction,
        string market,
        CancellationToken cancellationToken) =>
        _db.Rules.AsNoTracking().AnyAsync(
            rule => rule.CategoryId == categoryId
                    && rule.Jurisdiction == jurisdiction
                    && rule.Market == market
                    && rule.Status == TaxRuleStatus.Active,
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TaxClassificationSnapshot>> ListClassificationsByOfferIdsAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(offerIds);
        if (offerIds.Count == 0)
        {
            return [];
        }

        var ids = offerIds.Distinct().ToArray();
        var rows = await _db.OfferClassifications.AsNoTracking()
            .Where(x => ids.Contains(x.OfferId))
            .ToListAsync(cancellationToken);
        return rows.Select(row => new TaxClassificationSnapshot(row.OfferId, row.CategoryId)).ToArray();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TaxCategorySnapshot>> ListCategoriesByIdsAsync(
        IReadOnlyCollection<Guid> categoryIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(categoryIds);
        if (categoryIds.Count == 0)
        {
            return [];
        }

        var ids = categoryIds.Distinct().ToArray();
        var rows = await _db.Categories.AsNoTracking()
            .Where(x => ids.Contains(x.CategoryId))
            .ToListAsync(cancellationToken);
        return rows.Select(row => new TaxCategorySnapshot(row.CategoryId, row.Code, row.DisplayName)).ToArray();
    }
}
