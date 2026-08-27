using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.UserPreference.Application;
using Tooba.UserPreference.Infrastructure.Persistence;

namespace Tooba.UserPreference.Infrastructure;

/// <summary>ماژول مستقل ترجیح کاربر با schema، قرارداد و Outbox اختصاصی.</summary>
public sealed class UserPreferenceModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "UserPreference";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, UserPreferenceOutboxRegistration>();
        services.AddScoped<IUserPreferenceDirectory, UserPreferenceDirectory>();
        services.AddDbContext<UserPreferenceDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connection,
                UserPreferenceDbContext.Schema,
                typeof(UserPreferenceDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}

/// <summary>ثبت Outbox ترجیح کاربر؛ نسخهٔ فعلی رویداد بیرونی تعریف نمی‌کند.</summary>
public sealed class UserPreferenceOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => UserPreferenceDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(UserPreferenceDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) =>
        throw new InvalidOperationException("UserPreference integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
