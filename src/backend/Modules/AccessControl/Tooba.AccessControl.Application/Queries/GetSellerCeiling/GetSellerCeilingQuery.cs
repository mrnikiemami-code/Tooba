using MediatR;
using Tooba.AccessControl.Domain;

namespace Tooba.AccessControl.Application.Queries.GetSellerCeiling;

/// <summary>
/// پرس‌وجوی سقف مجوزهای فروشنده.
/// </summary>
/// <param name="SellerPartyId">شناسهٔ فروشنده.</param>
public sealed record GetSellerCeilingQuery(
    Guid SellerPartyId) : IRequest<IReadOnlyList<SellerCeilingEntryDto>>;

/// <summary>Handler پرس‌وجوی سقف فروشنده.</summary>
public sealed class GetSellerCeilingQueryHandler
    : IRequestHandler<GetSellerCeilingQuery, IReadOnlyList<SellerCeilingEntryDto>>
{
    private readonly IAccessControlDirectory _directory;

    /// <summary>سازنده.</summary>
    /// <param name="directory">دایرکتوری دسترسی.</param>
    public GetSellerCeilingQueryHandler(IAccessControlDirectory directory) => _directory = directory;

    /// <inheritdoc />
    public Task<IReadOnlyList<SellerCeilingEntryDto>> Handle(
        GetSellerCeilingQuery request, CancellationToken cancellationToken) =>
        _directory.GetSellerCeilingAsync(request.SellerPartyId, cancellationToken);
}
