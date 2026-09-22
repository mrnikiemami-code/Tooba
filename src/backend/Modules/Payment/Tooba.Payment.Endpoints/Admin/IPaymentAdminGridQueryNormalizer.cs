using Tooba.BuildingBlocks.Grid;

namespace Tooba.Payment.Endpoints.Admin;

/// <summary>Normalizes admin payment grid query (Host implements with existing whitelist policy).</summary>
public interface IPaymentAdminGridQueryNormalizer
{
    GridQueryRequest Normalize(GridQueryRequest request);
}
