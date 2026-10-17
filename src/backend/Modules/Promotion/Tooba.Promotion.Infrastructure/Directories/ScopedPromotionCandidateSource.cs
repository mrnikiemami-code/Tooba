using Microsoft.EntityFrameworkCore;
using Tooba.Promotion.Application.Promotions.Ports;
using Tooba.Promotion.Infrastructure.Persistence;

namespace Tooba.Promotion.Infrastructure.Directories;

/// <summary>
/// آداپتر scoped مالک خواندن جدول <c>promotion.promotions</c> برای ارزیابی.
/// خواندن واقعی در اولین ارزیابی هر scope انجام می‌شود و ارزیابی‌های بعدی همان تصویر را مصرف می‌کنند؛
/// هر جهش در همین scope تصویر را باطل می‌کند تا دادهٔ کهنه هرگز ارزیابی نشود.
/// </summary>
public sealed class ScopedPromotionCandidateSource : IPromotionCandidateSource
{
    private readonly PromotionCandidateSnapshot _snapshot;

    /// <summary>
    /// منبع را به DbContext مالک Promotion وصل می‌کند.
    /// </summary>
    public ScopedPromotionCandidateSource(PromotionDbContext db)
    {
        ArgumentNullException.ThrowIfNull(db);
        _snapshot = new PromotionCandidateSnapshot(async cancellationToken =>
            await db.Promotions.AsNoTracking().ToListAsync(cancellationToken));
    }

    /// <summary>تعداد خواندن‌های واقعی؛ مصرف تست/سنجش.</summary>
    public int LoadCount => _snapshot.LoadCount;

    /// <inheritdoc />
    public Task<IReadOnlyList<Domain.Aggregates.PromotionDefinition>> GetAsync(CancellationToken cancellationToken) =>
        _snapshot.GetAsync(cancellationToken);

    /// <inheritdoc />
    public void Invalidate() => _snapshot.Invalidate();
}
