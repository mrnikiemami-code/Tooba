using FluentValidation;
using Tooba.BulkInquiry.Application.Storefront.Commands;
using Tooba.BulkInquiry.Contracts.Errors;

namespace Tooba.BulkInquiry.Application.Storefront.Validators;

/// <summary>VALIDATOR_REQUIRED — envelope ورودی ثبت درخواست عمده.</summary>
public sealed class SubmitBulkInquiryCommandValidator : AbstractValidator<SubmitBulkInquiryCommand>
{
    /// <summary>قواعد حمل‌ونقل؛ قواعد دامنه در Domain می‌مانند.</summary>
    public SubmitBulkInquiryCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().WithErrorCode(BulkInquiryErrorCodes.RequestRequired);
        RuleFor(x => x.Request.ProductSlug).NotEmpty().WithErrorCode(BulkInquiryErrorCodes.SlugRequired);
        RuleFor(x => x.Request.FullName).NotEmpty().WithErrorCode(BulkInquiryErrorCodes.FullNameRequired);
        RuleFor(x => x.Request.Phone).NotEmpty().WithErrorCode(BulkInquiryErrorCodes.PhoneRequired);
        RuleFor(x => x.Request.Address).NotEmpty().WithErrorCode(BulkInquiryErrorCodes.AddressRequired);
    }
}
