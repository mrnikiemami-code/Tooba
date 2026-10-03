using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.ProductQnA.Application.Composition;
using Tooba.ProductQnA.Application.Models;
using Tooba.ProductQnA.Application.Ports;

namespace Tooba.ProductQnA.Application.Customer.Commands;

/// <summary>ثبت پرسش محصول برای Actor نشست.</summary>
public sealed record SubmitProductQuestionCommand(Guid ActorUserId, SubmitProductQuestion Body)
    : IRequest<Result<SubmitProductQuestionResult>>;

/// <summary>Handler ثبت پرسش.</summary>
public sealed class SubmitProductQuestionCommandHandler(IProductQaDirectory directory)
    : IRequestHandler<SubmitProductQuestionCommand, Result<SubmitProductQuestionResult>>
{
    /// <inheritdoc />
    public Task<Result<SubmitProductQuestionResult>> Handle(
        SubmitProductQuestionCommand request,
        CancellationToken cancellationToken) =>
        ProductQnAOperation.ExecuteAsync(async () =>
        {
            var id = await directory.SubmitQuestionAsync(request.ActorUserId, request.Body, cancellationToken);
            return new SubmitProductQuestionResult(id, "Pending");
        });
}
