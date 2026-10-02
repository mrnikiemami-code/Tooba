using MediatR;
using Tooba.BuildingBlocks.Results;
using Tooba.AccessControl.Application.Composition;
using Tooba.AccessControl.Domain;
using Tooba.AccessControl.Application.Models;
using Tooba.AccessControl.Application.Permissions;

namespace Tooba.AccessControl.Application.Queries.GetSellerCeiling;

/// <summary>
/// پرس‌وجوی سقف مجوزهای فروشنده.
/// </summary>
/// <param name="SellerPartyId">شناسهٔ فروشنده.</param>
public sealed record GetSellerCeilingQuery(
    Guid SellerPartyId) : IRequest<Result<IReadOnlyList<SellerCeilingEntryDto>>>;

/// <summary>Handler پرس‌وجوی سقف فروشنده.</summary>
public sealed class GetSellerCeilingQueryHandler
    : IRequestHandler<GetSellerCeilingQuery, Result<IReadOnlyList<SellerCeilingEntryDto>>>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public GetSellerCeilingQueryHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<Result<IReadOnlyList<SellerCeilingEntryDto>>> Handle(
        GetSellerCeilingQuery request, CancellationToken cancellationToken) =>
        AccessControlOperation.ExecuteAsync(() =>
            _directory.GetSellerCeilingAsync(request.SellerPartyId, cancellationToken));
}
