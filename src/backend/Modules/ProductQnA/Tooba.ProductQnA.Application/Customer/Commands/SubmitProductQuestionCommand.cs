using MediatR;
using Tooba.ProductQnA.Application.Models;
using Tooba.ProductQnA.Application.Ports;

namespace Tooba.ProductQnA.Application.Customer.Commands;

/// <summary>ثبت پرسش محصول برای Actor نشست.</summary>
public sealed record SubmitProductQuestionCommand(Guid ActorUserId, SubmitProductQuestion Body) : IRequest<Guid>;

/// <summary>Handler ثبت پرسش.</summary>
public sealed class SubmitProductQuestionCommandHandler(IProductQaDirectory directory)
    : IRequestHandler<SubmitProductQuestionCommand, Guid>
{
    /// <inheritdoc />
    public Task<Guid> Handle(SubmitProductQuestionCommand request, CancellationToken cancellationToken)
        => directory.SubmitQuestionAsync(request.ActorUserId, request.Body, cancellationToken);
}
