using MediatR;
using Tooba.BulkInquiry.Application.Models;
using Tooba.BulkInquiry.Application.Ports;

namespace Tooba.BulkInquiry.Application.Storefront.Commands;

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
