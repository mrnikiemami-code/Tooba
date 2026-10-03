namespace Tooba.BulkInquiry.Application.Models;

/// <summary>ورودی ثبت درخواست خرید عمده؛ slug محصول از مسیر HTTP تأمین می‌شود.</summary>
public sealed record SubmitBulkInquiryRequest(
    string ProductSlug,
    string FullName,
    string Phone,
    string? Email,
    string? CompanyName,
    string Address,
    decimal Quantity,
    string? Notes);

/// <summary>نتیجهٔ ثبت درخواست خرید عمده.</summary>
public sealed record SubmitBulkInquiryResult(Guid InquiryId, string Status);
