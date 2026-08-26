namespace Tooba.BulkInquiry.Application;

/// <summary>ورودی ثبت درخواست خرید عمده؛ slug محصول از مسیر HTTP تأمین می‌شود.</summary>
public sealed record SubmitBulkInquiryRequest(
    string ProductSlug,
    string FullName,
    string Phone,
    string? Email,
    string? CompanyName,
    string Address,
    int Quantity,
    string? Notes);

/// <summary>قابلیت کاربردی ثبت درخواست خرید عمده.</summary>
public interface IBulkInquiryDirectory
{
    /// <summary>درخواست را برای محصول منتشرشده ثبت می‌کند.</summary>
    Task<Guid> SubmitAsync(SubmitBulkInquiryRequest request, CancellationToken cancellationToken);
}
