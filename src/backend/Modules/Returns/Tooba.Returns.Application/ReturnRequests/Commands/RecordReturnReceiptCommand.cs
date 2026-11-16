using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.Returns.Application.Composition;
using Tooba.Returns.Application.ReturnRequests.Models;
using Tooba.Returns.Application.ReturnRequests.Ports;
using Tooba.Returns.Domain.ValueObjects;

namespace Tooba.Returns.Application.ReturnRequests.Commands;

/// <summary>
/// TB-274: records ONE physical customer receipt occurrence of a delivered-customer RMA — what actually arrived and
/// how much of it is usable versus damaged.
/// <para>
/// The occurrence key is the durable identity of the delivery: the same key with the same lines replays the stored
/// receipt, a different intent under that key is a typed conflict. Recording a receipt moves no money and no stock;
/// it only states the physical fact every later gate (approval, refund attempt, restock) reads.
/// </para>
/// </summary>
public sealed record RecordReturnReceiptCommand(
    Guid ReturnRequestId,
    Guid ActorUserId,
    string OccurrenceKey,
    IReadOnlyList<CustomerReceiptLine> Items) : IRequest<Result<ReturnSnapshot>>;

/// <summary>
/// TB-274: one inspected line of a submitted customer receipt.
/// <para>
/// TB-274-R1: the inspection is REQUIRED. There is deliberately no default, because an omitted condition must never be
/// read as <see cref="ReturnItemCondition.Usable"/>; every caller has to name what the operator actually inspected.
/// </para>
/// </summary>
public sealed record CustomerReceiptLine(
    Guid ReturnItemId,
    decimal Quantity,
    ReturnItemCondition Condition);

/// <summary>Handler — Result via the canonical typed-fault seam.</summary>
public sealed class RecordReturnReceiptHandler(IReturnDirectory returns)
    : IRequestHandler<RecordReturnReceiptCommand, Result<ReturnSnapshot>>
{
    public Task<Result<ReturnSnapshot>> Handle(
        RecordReturnReceiptCommand request,
        CancellationToken cancellationToken) =>
        ReturnsOperation.ExecuteAsync(() => returns.RecordReceiptAsync(request, cancellationToken));
}
