using FluentValidation;
using MediatR;
using Tooba.BulkInquiry.Application;

namespace Tooba.BulkInquiry.Application.Commands;

/// <summary>ثبت درخواست خرید عمده برای slug محصول.</summary>
public sealed record SubmitBulkInquiryCommand(SubmitBulkInquiryRequest Request) : IRequest<Guid>;

/// <summary>Handler ثبت درخواست عمده.</summary>
public sealed class SubmitBulkInquiryCommandHandler(IBulkInquiryDirectory directory)
    : IRequestHandler<SubmitBulkInquiryCommand, Guid>
{
    /// <inheritdoc />
    public Task<Guid> Handle(SubmitBulkInquiryCommand request, CancellationToken cancellationToken)
        => directory.SubmitAsync(request.Request, cancellationToken);
}

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
