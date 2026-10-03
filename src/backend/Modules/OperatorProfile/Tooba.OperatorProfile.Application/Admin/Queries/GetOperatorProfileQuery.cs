using MediatR;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks.Results;
using Tooba.OperatorProfile.Application.Composition;
using Tooba.OperatorProfile.Application.Models;
using Tooba.OperatorProfile.Application.Ports;

namespace Tooba.OperatorProfile.Application.Admin.Queries;

/// <summary>خواندن پروفایل اپراتور برای Actor مجاز.</summary>
public sealed record GetOperatorProfileQuery(Guid ActorUserId) : IRequest<Result<OperatorProfileAdminResponse>>;

/// <summary>Handler خواندن پروفایل اپراتور.</summary>
public sealed class GetOperatorProfileQueryHandler(
    IOperatorProfileDirectory directory,
    ILogger<GetOperatorProfileQueryHandler> logger)
    : IRequestHandler<GetOperatorProfileQuery, Result<OperatorProfileAdminResponse>>
{
    /// <inheritdoc />
    public async Task<Result<OperatorProfileAdminResponse>> Handle(
        GetOperatorProfileQuery request,
        CancellationToken cancellationToken)
    {
        var outcome = await OperatorProfileOperation.ExecuteAsync(async () =>
        {
            var snapshot = await directory.GetAsync(request.ActorUserId, cancellationToken);
            return snapshot is null
                ? OperatorProfileAdminResponse.Empty
                : OperatorProfileAdminResponse.FromSnapshot(snapshot);
        });

        if (outcome.IsFailure)
        {
            logger.LogInformation("operator.profile.get.failed");
            return outcome;
        }

        logger.LogInformation("operator.profile.get.succeeded");
        return outcome;
    }
}
