using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.ModuleContracts;

namespace Tooba.ProductWorkspace.Infrastructure;

/// <summary>
/// ProductWorkspace module composition entry. W18 registers no persistence,
/// foreign database contexts, or business services — behavior-neutral skeleton only.
/// </summary>
public sealed class ProductWorkspaceModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "ProductWorkspace";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
        // W18: intentional no-op. Aggregate composition adapters arrive in later waves.
    }
}
