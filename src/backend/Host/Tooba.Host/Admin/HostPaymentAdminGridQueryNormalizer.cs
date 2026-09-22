#pragma warning disable CS1591
using Tooba.BuildingBlocks.Grid;
using Tooba.Host.Grid;
using Tooba.Payment.Endpoints.Admin;

namespace Tooba.Host.Admin;

/// <summary>Host adapter: reuse existing Admin payments grid whitelist policy.</summary>
public sealed class HostPaymentAdminGridQueryNormalizer : IPaymentAdminGridQueryNormalizer
{
    public GridQueryRequest Normalize(GridQueryRequest request) =>
        AdminListGridPolicies.Payments.Normalize(request);
}
