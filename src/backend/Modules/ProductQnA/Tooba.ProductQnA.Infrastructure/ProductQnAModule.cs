using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.ProductQnA.Application.Ports;
using Tooba.ProductQnA.Contracts.Errors;
using Tooba.ProductQnA.Infrastructure.Directories;
using Tooba.ProductQnA.Infrastructure.Persistence;

namespace Tooba.ProductQnA.Infrastructure;

/// <summary>ماژول مستقل ProductQnA و schema اختصاصی آن.</summary>
public sealed class ProductQnAModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "ProductQnA";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, ProductQnAOutboxRegistration>();
        services.AddSingleton<IErrorCatalogContributor, ProductQnAErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, ProductQnAErrorResourceSet>();
        services.AddScoped<IProductQaDirectory, ProductQaDirectory>();
        services.AddModuleSchemaMigrator<ProductQnADbContext>("ProductQnA", ModuleSchemaMigrationOrder.ProductQnA);
        services.AddDbContext<ProductQnADbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(sp.GetRequiredService<ICurrentCommerceContext>(), sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(options, connection, ProductQnADbContext.Schema, typeof(ProductQnADbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
