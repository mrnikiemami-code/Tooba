using Tooba.BulkInquiry.Application.Models;

namespace Tooba.BulkInquiry.Application.Ports;

/// <summary>قابلیت کاربردی ثبت درخواست خرید عمده.</summary>
public interface IBulkInquiryDirectory
{
    /// <summary>درخواست را برای محصول منتشرشده ثبت می‌کند.</summary>
    Task<Guid> SubmitAsync(SubmitBulkInquiryRequest request, CancellationToken cancellationToken);
}
