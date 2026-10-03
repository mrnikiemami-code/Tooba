using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BulkInquiry.Application.Ports;
using Tooba.BulkInquiry.Contracts.Errors;
using Tooba.BulkInquiry.Infrastructure.Directories;
using Tooba.BulkInquiry.Infrastructure.Persistence;
using Tooba.ModuleContracts;
using Tooba.Persistence;

namespace Tooba.BulkInquiry.Infrastructure;

/// <summary>ماژول مستقل BulkInquiry و schema اختصاصی آن.</summary>
public sealed class BulkInquiryModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "BulkInquiry";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, BulkInquiryOutboxRegistration>();
        services.AddSingleton<IErrorCatalogContributor, BulkInquiryErrorCatalogContributor>();
        services.AddSingleton<IErrorResourceSet, BulkInquiryErrorResourceSet>();
        services.AddScoped<IBulkInquiryDirectory, BulkInquiryDirectory>();
        services.AddModuleSchemaMigrator<BulkInquiryDbContext>("BulkInquiry", ModuleSchemaMigrationOrder.BulkInquiry);
        services.AddDbContext<BulkInquiryDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(sp.GetRequiredService<ICurrentCommerceContext>(), sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(options, connection, BulkInquiryDbContext.Schema, typeof(BulkInquiryDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
