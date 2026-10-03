using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.BulkInquiry.Application.Composition;
using Tooba.BulkInquiry.Application.Models;
using Tooba.BulkInquiry.Application.Ports;

namespace Tooba.BulkInquiry.Application.Storefront.Commands;

/// <summary>ثبت درخواست خرید عمده برای slug محصول.</summary>
public sealed record SubmitBulkInquiryCommand(SubmitBulkInquiryRequest Request)
    : IRequest<Result<SubmitBulkInquiryResult>>;

/// <summary>Handler ثبت درخواست عمده.</summary>
public sealed class SubmitBulkInquiryCommandHandler(IBulkInquiryDirectory directory)
    : IRequestHandler<SubmitBulkInquiryCommand, Result<SubmitBulkInquiryResult>>
{
    /// <inheritdoc />
    public Task<Result<SubmitBulkInquiryResult>> Handle(
        SubmitBulkInquiryCommand request,
        CancellationToken cancellationToken) =>
        BulkInquiryOperation.ExecuteAsync(async () =>
        {
            var id = await directory.SubmitAsync(request.Request, cancellationToken);
            return new SubmitBulkInquiryResult(id, "Submitted");
        });
}
