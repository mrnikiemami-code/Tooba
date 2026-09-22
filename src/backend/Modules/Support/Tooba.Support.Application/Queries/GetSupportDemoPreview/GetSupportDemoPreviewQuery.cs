using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Errors;
using Tooba.Support.Application.Models;
using Tooba.Support.Application.Ports;

namespace Tooba.Support.Application.Queries.GetSupportDemoPreview;

/// <summary>MediatR Support admin demo-preview use case.</summary>
public sealed record GetSupportDemoPreviewQuery : IRequest<Result<SupportDemoSnapshotDto>>;

/// <summary>Reads published demo snapshot; not-ready → SemanticError.</summary>
public sealed class GetSupportDemoPreviewHandler(ISupportDemoPreviewPort demo)
    : IRequestHandler<GetSupportDemoPreviewQuery, Result<SupportDemoSnapshotDto>>
{
    public Task<Result<SupportDemoSnapshotDto>> Handle(
        GetSupportDemoPreviewQuery request, CancellationToken cancellationToken)
    {
        var snapshot = demo.TryGetCurrent();
        return Task.FromResult(snapshot is null
            ? Result.Failure<SupportDemoSnapshotDto>(new SemanticError(SupportErrorCodes.DemoNotReady))
            : Result.Success(snapshot));
    }
}
