using MediatR;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Results;
using Tooba.Support.Application.Tickets.Models;
using Tooba.Support.Application.Tickets.Ports;
using Tooba.Support.Contracts.Errors;

namespace Tooba.Support.Application.Tickets.Queries;

/// <summary>مورد استفادهٔ MediatR برای پیش‌نمایش demo پشتیبانی (مدیر).</summary>
public sealed record GetSupportDemoPreviewQuery : IRequest<Result<SupportDemoSnapshotDto>>;

/// <summary>snapshot منتشرشدهٔ demo را می‌خواند؛ حالت آماده‌نبودن → <c>SemanticError</c>.</summary>
public sealed class GetSupportDemoPreviewHandler(ISupportDemoPreviewPort demo)
    : IRequestHandler<GetSupportDemoPreviewQuery, Result<SupportDemoSnapshotDto>>
{
    /// <inheritdoc />
    public Task<Result<SupportDemoSnapshotDto>> Handle(
        GetSupportDemoPreviewQuery request, CancellationToken cancellationToken)
    {
        var snapshot = demo.TryGetCurrent();
        return Task.FromResult(snapshot is null
            ? Result.Failure<SupportDemoSnapshotDto>(new SemanticError(SupportErrorCodes.DemoNotReady))
            : Result.Success(snapshot));
    }
}
