using MediatR;

namespace Tooba.AccessControl.Application.Development.Seller;

/// <summary>
/// خواندن نگاشت Actor↔Seller توسعهٔ فروشنده برای مسیر <c>GET /v1/seller/dev-contexts</c>.
/// </summary>
public sealed record GetSellerDevContextsQuery : IRequest<SellerDevContextsView?>;
