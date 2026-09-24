using Tooba.StoreContext.Contracts.Current;

namespace Tooba.StoreContext.Infrastructure.Current;

/// <summary>
/// Scoped accessor for the effective store commerce context. It implements both the read seam and
/// the assignment seam, holds no static mutable state, depends on no HttpContext, and uses no
/// AsyncLocal: the lifecycle is one DI scope (request or worker cycle).
/// </summary>
public sealed class StoreCommerceContextAccessor : ICurrentStoreCommerceContext, IStoreCommerceContextAssigner
{
    private StoreCommerceContext? _current;

    /// <inheritdoc />
    public StoreCommerceContext? Current => _current;

    /// <inheritdoc />
    public void Assign(StoreCommerceContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _current = context;
    }
}
