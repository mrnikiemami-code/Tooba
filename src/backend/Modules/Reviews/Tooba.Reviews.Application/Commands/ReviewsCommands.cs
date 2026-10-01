using MediatR;
using Tooba.Reviews.Application.Presentation;

namespace Tooba.Reviews.Application.Commands;

/// <summary>ثبت نظر مشتری.</summary>
public sealed record SubmitProductReviewCommand(Guid ActorUserId, SubmitProductReview Input) : IRequest<Guid>;

/// <summary>Handler ثبت نظر.</summary>
public sealed class SubmitProductReviewCommandHandler(ReviewsPresentationComposer composer)
    : IRequestHandler<SubmitProductReviewCommand, Guid>
{
    /// <inheritdoc />
    public Task<Guid> Handle(SubmitProductReviewCommand request, CancellationToken cancellationToken)
        => composer.SubmitAsync(request.ActorUserId, request.Input, cancellationToken);
}

/// <summary>انتشار نظر Pending.</summary>
public sealed record PublishAdminReviewCommand(Guid ReviewId, Guid ModeratorUserId) : IRequest<MediatR.Unit>;

/// <summary>Handler انتشار.</summary>
public sealed class PublishAdminReviewCommandHandler(ReviewsPresentationComposer composer)
    : IRequestHandler<PublishAdminReviewCommand, MediatR.Unit>
{
    /// <inheritdoc />
    public async Task<MediatR.Unit> Handle(PublishAdminReviewCommand request, CancellationToken cancellationToken)
    {
        await composer.PublishAsync(request.ReviewId, request.ModeratorUserId, cancellationToken);
        return MediatR.Unit.Value;
    }
}

/// <summary>رد نظر Pending.</summary>
public sealed record RejectAdminReviewCommand(Guid ReviewId, Guid ModeratorUserId, string Reason) : IRequest<MediatR.Unit>;

/// <summary>Handler رد.</summary>
public sealed class RejectAdminReviewCommandHandler(ReviewsPresentationComposer composer)
    : IRequestHandler<RejectAdminReviewCommand, MediatR.Unit>
{
    /// <inheritdoc />
    public async Task<MediatR.Unit> Handle(RejectAdminReviewCommand request, CancellationToken cancellationToken)
    {
        await composer.RejectAsync(request.ReviewId, request.ModeratorUserId, request.Reason, cancellationToken);
        return MediatR.Unit.Value;
    }
}
