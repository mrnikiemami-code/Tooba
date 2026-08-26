using Tooba.BulkInquiry.Application;
using Tooba.BulkInquiry.Domain;
using Tooba.BulkInquiry.Infrastructure.Persistence;
using Tooba.Catalog.Application;
using Tooba.Catalog.Domain;

namespace Tooba.BulkInquiry.Infrastructure;

/// <summary>دایرکتوری BulkInquiry با خواندن فقط از قرارداد Catalog و schema خودش.</summary>
public sealed class BulkInquiryDirectory : IBulkInquiryDirectory
{
    private readonly BulkInquiryDbContext _db;
    private readonly ICatalogLookupGateway _catalog;

    /// <summary>وابستگی‌های مالک را تزریق می‌کند.</summary>
    public BulkInquiryDirectory(BulkInquiryDbContext db, ICatalogLookupGateway catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<Guid> SubmitAsync(SubmitBulkInquiryRequest request, CancellationToken cancellationToken)
    {
        var product = await _catalog.FindReviewableProductBySlugAsync(request.ProductSlug, cancellationToken);
        if (product is null || product.Status != CatalogPublicationStatus.Published)
            throw new InvalidOperationException("محصول منتشرشده پیدا نشد.");

        var inquiry = BulkPurchaseInquiry.Create(
            product.ProductId,
            request.FullName,
            request.Phone,
            request.Email,
            request.CompanyName,
            request.Address,
            request.Quantity,
            request.Notes,
            DateTimeOffset.UtcNow);

        _db.Inquiries.Add(inquiry);
        await _db.SaveChangesAsync(cancellationToken);
        return inquiry.InquiryId;
    }
}
