using MediatR;
using Tooba.OperatorProfile.Application;

namespace Tooba.OperatorProfile.Application.Admin.Queries;

/// <summary>خواندن پروفایل اپراتور برای Actor مجاز.</summary>
public sealed record GetOperatorProfileQuery(Guid ActorUserId) : IRequest<OperatorProfileSnapshot?>;

/// <summary>Handler خواندن پروفایل اپراتور.</summary>
public sealed class GetOperatorProfileQueryHandler(IOperatorProfileDirectory directory)
    : IRequestHandler<GetOperatorProfileQuery, OperatorProfileSnapshot?>
{
    /// <inheritdoc />
    public Task<OperatorProfileSnapshot?> Handle(GetOperatorProfileQuery request, CancellationToken cancellationToken)
        => directory.GetAsync(request.ActorUserId, cancellationToken);
}
