using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.ModuleContracts;
using Tooba.PageComposition.Application.Composition;
using Tooba.PageComposition.Application.Ports;
using Tooba.PageComposition.Contracts.Errors;
using Tooba.PageComposition.Infrastructure.Directories;
using Tooba.PageComposition.Infrastructure.Persistence;
using Tooba.Persistence;

namespace Tooba.PageComposition.Infrastructure;

/// <summary>ماژول مستقل Page Composition و schema اختصاصی آن.</summary>
public sealed class PageCompositionModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "PageComposition";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, PageCompositionOutboxRegistration>();
        services.AddSingleton<IErrorCatalogContributor, PageCompositionErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, PageCompositionErrorResourceSet>();
        services.AddScoped<IPageCompositionDirectory, PageCompositionDirectory>();
        services.AddScoped<PageCompositionPresentationComposer>();
        services.AddModuleSchemaMigrator<PageCompositionDbContext>("PageComposition", ModuleSchemaMigrationOrder.PageComposition);
        services.AddDbContext<PageCompositionDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connection,
                PageCompositionDbContext.Schema,
                typeof(PageCompositionDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}

/// <summary>ثبت Outbox PageComposition؛ نسخهٔ پایه هنوز رویداد بیرونی منتشر نمی‌کند.</summary>
public sealed class PageCompositionOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => PageCompositionDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(PageCompositionDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("PageComposition integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
