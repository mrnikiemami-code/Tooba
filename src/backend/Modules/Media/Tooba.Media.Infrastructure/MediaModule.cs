using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Media.Application;
using Tooba.Media.Infrastructure.Persistence;
using Tooba.ModuleContracts;
using Tooba.Persistence;

namespace Tooba.Media.Infrastructure;

/// <summary>ماژول مستقل Media و schema اختصاصی آن.</summary>
public sealed class MediaModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Media";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, MediaOutboxRegistration>();
        services.AddSingleton<IMediaObjectStore>(sp =>
        {
            var env = sp.GetRequiredService<IHostEnvironment>();
            var config = sp.GetRequiredService<IConfiguration>();
            var configured = config["Tooba:Media:LocalRoot"];
            var root = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(env.ContentRootPath, "App_Data", "media")
                : Path.IsPathRooted(configured)
                    ? configured
                    : Path.Combine(env.ContentRootPath, configured);
            return new LocalFileMediaStore(root);
        });
        services.AddScoped<IMediaDirectory, MediaDirectory>();
        services.AddDbContext<MediaDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(options, connection, MediaDbContext.Schema, typeof(MediaDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}

/// <summary>ثبت Outbox Media؛ نسخهٔ پایه هنوز رویداد بیرونی منتشر نمی‌کند.</summary>
public sealed class MediaOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => MediaDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(MediaDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("Media integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
