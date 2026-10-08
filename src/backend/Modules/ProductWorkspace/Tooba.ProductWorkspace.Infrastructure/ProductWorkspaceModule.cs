using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Localization;
using Tooba.ModuleContracts;
using Tooba.ProductWorkspace.Contracts.Errors;

namespace Tooba.ProductWorkspace.Infrastructure;

/// <summary>
/// ProductWorkspace module composition entry. The module owns no persistence, no DbContext and no schema:
/// it is an Admin composed product surface over other modules' Contracts. W1 registers the module-owned
/// error resource set only; the descriptors for the shared <c>workspace.*</c> codes stay owned once by
/// <c>CatalogErrorCatalogContributor</c>, so no duplicate catalog contributor and no foreign database
/// context or business service is introduced.
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
        services.AddSingleton<IErrorResourceSet, ProductWorkspaceErrorResourceSet>();
    }
}
