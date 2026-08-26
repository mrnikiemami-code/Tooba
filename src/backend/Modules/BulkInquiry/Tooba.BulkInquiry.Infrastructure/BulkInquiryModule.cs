using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.BulkInquiry.Application;
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
        services.AddScoped<IBulkInquiryDirectory, BulkInquiryDirectory>();
        services.AddDbContext<BulkInquiryDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(sp.GetRequiredService<ICurrentCommerceContext>(), sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(options, connection, BulkInquiryDbContext.Schema, typeof(BulkInquiryDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}

/// <summary>ثبت Outbox BulkInquiry؛ نسخهٔ پایه هنوز رویداد بیرونی منتشر نمی‌کند.</summary>
public sealed class BulkInquiryOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => BulkInquiryDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(BulkInquiryDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) => throw new InvalidOperationException("BulkInquiry integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
