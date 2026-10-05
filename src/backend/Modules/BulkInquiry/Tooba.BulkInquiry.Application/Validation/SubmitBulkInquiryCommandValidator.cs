using FluentValidation;
using Tooba.BulkInquiry.Application.Storefront.Commands;

namespace Tooba.BulkInquiry.Application.Validation;

/// <summary>VALIDATOR_REQUIRED — envelope ورودی ثبت درخواست عمده.</summary>
public sealed class SubmitBulkInquiryCommandValidator : AbstractValidator<SubmitBulkInquiryCommand>
{
    /// <summary>قواعد حمل‌ونقل؛ قواعد دامنه در Domain می‌مانند.</summary>
    public SubmitBulkInquiryCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithErrorCode(BulkInquiryValidationCodes.RequestRequired);
        RuleFor(x => x.Request.ProductSlug).NotEmpty().WithErrorCode(BulkInquiryValidationCodes.SlugRequired);
        RuleFor(x => x.Request.FullName).NotEmpty().WithErrorCode(BulkInquiryValidationCodes.FullNameRequired);
        RuleFor(x => x.Request.Phone).NotEmpty().WithErrorCode(BulkInquiryValidationCodes.PhoneRequired);
        RuleFor(x => x.Request.Address).NotEmpty().WithErrorCode(BulkInquiryValidationCodes.AddressRequired);
    }
}
