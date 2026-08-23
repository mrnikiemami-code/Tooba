using Microsoft.EntityFrameworkCore;
using Tooba.Tax.Application;
using Tooba.Tax.Domain;
using Tooba.Tax.Infrastructure.Persistence;

namespace Tooba.Tax.Infrastructure;

/// <summary>
/// نگهبان باز موردکاربرد Tax.
/// </summary>
public sealed class OpenTaxUseCaseGuard : ITaxUseCaseGuard
{
    /// <inheritdoc />
    public Task EnsureCanMutateAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// پیکربندی و محاسبهٔ مالیات در schema tax. Pricing و Order را بازنویسی نمی‌کند.
/// </summary>
public sealed class TaxDirectory : ITaxDirectory
{
    private readonly TaxDbContext _db;
    private readonly ITaxUseCaseGuard _guard;

    /// <summary>
    /// دایرکتوری را به schema tax وصل می‌کند.
    /// </summary>
    public TaxDirectory(TaxDbContext db, ITaxUseCaseGuard guard)
    {
        _db = db;
        _guard = guard;
    }

    /// <inheritdoc />
    public async Task<TaxCategoryReference> CreateCategoryAsync(string code, string displayName, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var category = TaxCategory.Create(code, displayName, DateTimeOffset.UtcNow);
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
            throw new InvalidOperationException("طبقهٔ مالیاتی پیدا نشد.");
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
            throw new InvalidOperationException("طبقهٔ مالیاتی برای قاعده پیدا نشد.");
        }

        var rule = TaxRule.Create(
            jurisdiction,
            market,
            categoryId,
            kind,
            rate,
            effectiveFrom,
            effectiveTo,
            specificity,
            overridePolicy,
            DateTimeOffset.UtcNow);
        _db.Rules.Add(rule);
        await _db.SaveChangesAsync(cancellationToken);
        return ToRuleReference(rule);
    }

    /// <inheritdoc />
    public async Task ActivateRuleAsync(Guid ruleId, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var rule = await _db.Rules.SingleAsync(x => x.RuleId == ruleId, cancellationToken);
        rule.Activate(DateTimeOffset.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ChangeRuleRateAsync(Guid ruleId, decimal rate, CancellationToken cancellationToken)
    {
        await _guard.EnsureCanMutateAsync(cancellationToken);
        var rule = await _db.Rules.SingleAsync(x => x.RuleId == ruleId, cancellationToken);
        rule.ChangeRate(rate, DateTimeOffset.UtcNow);
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

        var lineExclusive = TaxRounding.Round(exclusive * request.Quantity, request.Currency);
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
                taxAmount = TaxRounding.Round(lineExclusive * rate, request.Currency);
                outcome = TaxOutcome.Taxable;
                break;
        }

        return new TaxCalculationResult(
            outcome,
            lineExclusive,
            rate,
            taxAmount,
            TaxRounding.Round(lineExclusive + taxAmount, request.Currency),
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
}
