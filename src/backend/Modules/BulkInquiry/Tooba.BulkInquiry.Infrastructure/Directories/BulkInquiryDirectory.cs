using Tooba.BuildingBlocks;
using Tooba.BulkInquiry.Application.Models;
using Tooba.BulkInquiry.Application.Ports;
using Tooba.BulkInquiry.Contracts.Errors;
using Tooba.BulkInquiry.Domain.Aggregates;
using Tooba.BulkInquiry.Infrastructure.Persistence;
using Tooba.Catalog.Contracts;
using Tooba.Catalog.Contracts.Ports;

namespace Tooba.BulkInquiry.Infrastructure.Directories;

/// <summary>دایرکتوری BulkInquiry با خواندن فقط از Catalog.Contracts و schema خودش.</summary>
public sealed class BulkInquiryDirectory : IBulkInquiryDirectory
{
    private readonly BulkInquiryDbContext _db;
    private readonly ICatalogReviewProductLookup _catalog;

    /// <summary>وابستگی‌های مالک را تزریق می‌کند.</summary>
    public BulkInquiryDirectory(BulkInquiryDbContext db, ICatalogReviewProductLookup catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    /// <inheritdoc />
    public async Task<Guid> SubmitAsync(SubmitBulkInquiryRequest request, CancellationToken cancellationToken)
    {
        var product = await _catalog.FindBySlugAsync(request.ProductSlug, cancellationToken);
        if (product is null || !string.Equals(product.Status, "Published", StringComparison.Ordinal))
            throw new SemanticException(new SemanticError(BulkInquiryErrorCodes.Rejected));

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
