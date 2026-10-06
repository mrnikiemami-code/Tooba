using MediatR;
using Microsoft.Extensions.Logging;
using Tooba.BuildingBlocks.Results;
using Tooba.OperatorProfile.Application.Composition;
using Tooba.OperatorProfile.Application.Models;
using Tooba.OperatorProfile.Application.Ports;
using Tooba.OperatorProfile.Contracts.Errors;

namespace Tooba.OperatorProfile.Application.Admin.Commands;

/// <summary>ایجاد/به‌روزرسانی پروفایل اپراتور برای Actor مجاز.</summary>
public sealed record UpsertOperatorProfileCommand(
    Guid ActorUserId,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? Bio) : IRequest<Result<OperatorProfileAdminResponse>>;

/// <summary>Handler نوشتن پروفایل اپراتور.</summary>
public sealed class UpsertOperatorProfileCommandHandler(
    IOperatorProfileDirectory directory,
    ILogger<UpsertOperatorProfileCommandHandler> logger)
    : IRequestHandler<UpsertOperatorProfileCommand, Result<OperatorProfileAdminResponse>>
{
    /// <inheritdoc />
    public async Task<Result<OperatorProfileAdminResponse>> Handle(
        UpsertOperatorProfileCommand request,
        CancellationToken cancellationToken)
    {
        var outcome = await OperatorProfileOperation.ExecuteAsync(async () =>
        {
            var updated = await directory.UpsertAsync(
                request.ActorUserId,
                new OperatorProfileWrite(request.DisplayName, request.FirstName, request.LastName, request.Bio),
                cancellationToken);
            return OperatorProfileAdminResponse.FromSnapshot(updated);
        });

        if (outcome.IsFailure)
        {
            logger.LogInformation("{OperatorProfileUpsertEvent}", OperatorProfileErrorCodes.ProfileRejected);
            return outcome;
        }

        logger.LogInformation("operator.profile.upsert.succeeded");
        return outcome;
    }
}
