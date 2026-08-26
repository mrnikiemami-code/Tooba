using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Catalog.Application;
using Tooba.ProductQnA.Domain;
using Tooba.ProductQnA.Infrastructure.Persistence;

namespace Tooba.ProductQnA.Infrastructure;

/// <summary>دانهٔ توسعهٔ قطعی و idempotent برای پرسش و پاسخ نمایشی.</summary>
public static class ProductQnADevelopmentSeed
{
    /// <summary>
    /// دو پرسش Published با پاسخ برای محصول demo-mobile-1 درج می‌کند.
    /// فراخواننده باید CommerceContext را روی همین scope تنظیم کرده باشد
    /// و پس از دانهٔ فروشگاه (وجود demo-mobile-1) صدا بزند.
    /// </summary>
    public static async Task ApplyAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var db = services.GetRequiredService<ProductQnADbContext>();
        var catalog = services.GetRequiredService<ICatalogLookupGateway>();
        var product = await catalog.FindReviewableProductBySlugAsync("demo-mobile-1", cancellationToken);
        if (product is null) return;

        var rows = new[]
        {
            (Guid.Parse("13000000-0000-4000-8000-000000000001"), "رضا", "آیا این مدل دو سیم‌کارت دارد؟", "پشتیبانی توبا", "بله، این مدل دو سیم‌کارت فعال دارد."),
            (Guid.Parse("13000000-0000-4000-8000-000000000002"), "نیلوفر", "گارانتی محصول چند ماه است؟", "پشتیبانی توبا", "گارانتی ۱۸ ماهه شرکتی است."),
        };
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);
        var moderator = Guid.Parse("12000000-0000-4000-8000-000000000099");

        foreach (var row in rows)
        {
            if (await db.Questions.AnyAsync(x => x.ProductId == product.ProductId && x.AuthorUserId == row.Item1, cancellationToken))
                continue;

            var question = ProductQuestion.Create(product.ProductId, row.Item1, row.Item2, row.Item3, now);
            question.Publish(moderator, now);
            db.Questions.Add(question);

            var answer = ProductAnswer.Create(question.QuestionId, row.Item4, row.Item5, now);
            answer.Publish();
            db.Answers.Add(answer);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
