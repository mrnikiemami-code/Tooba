using FluentValidation;
using Tooba.BulkInquiry.Application.Storefront.Commands;

namespace Tooba.BulkInquiry.Application.Storefront.Validators;

/// <summary>اعتبارسنجی حمل‌ونقل درخواست عمده.</summary>
public sealed class SubmitBulkInquiryCommandValidator : AbstractValidator<SubmitBulkInquiryCommand>
{
    /// <summary>قواعد حمل‌ونقل؛ قواعد دامنه در Domain می‌مانند.</summary>
    public SubmitBulkInquiryCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithErrorCode("bulk_inquiry.validation.request_required");
        RuleFor(x => x.Request.ProductSlug).NotEmpty().WithErrorCode("bulk_inquiry.validation.slug_required");
        RuleFor(x => x.Request.FullName).NotEmpty().WithErrorCode("bulk_inquiry.validation.full_name_required");
        RuleFor(x => x.Request.Phone).NotEmpty().WithErrorCode("bulk_inquiry.validation.phone_required");
        RuleFor(x => x.Request.Address).NotEmpty().WithErrorCode("bulk_inquiry.validation.address_required");
    }
}
