using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.ModuleContracts;
using Tooba.Persistence;
using Tooba.Story.Application.Stories.Ports;
using Tooba.Story.Application.Stories.Presentation;
using Tooba.Story.Infrastructure.Adapters;
using Tooba.Story.Infrastructure.Directories;
using Tooba.Story.Infrastructure.Messaging;
using Tooba.Story.Infrastructure.Persistence;

namespace Tooba.Story.Infrastructure.DependencyInjection;

/// <summary>
/// ریشهٔ ترکیب (composition root) ماژول مستقل Story و schema اختصاصی آن.
/// این کلاس فقط ثبت سرویس‌ها و DbContext را بر عهده دارد؛ منطق کسب‌وکار، Endpoint و Persistence
/// در همین ماژول و در پوشه‌های مسئولیت‌محور خود قرار دارند و Host هیچ مالکیتی بر آن‌ها ندارد.
/// </summary>
public sealed class StoryModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Story";

    /// <summary>
    /// سرویس‌های ماژول Story را ثبت می‌کند: دایرکتوری، Adapter گرید، Composer ارائه و DbContext با schema مستقل.
    /// اتصال از زمینهٔ تجارت جاری resolve می‌شود و interceptor سراسری Outbox روی همین context سوار می‌گردد.
    /// </summary>
    /// <param name="services">مجموعهٔ سرویس‌های میزبان.</param>
    /// <param name="configuration">پیکربندی میزبان.</param>
    /// <param name="environment">محیط اجرای میزبان.</param>
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, StoryOutboxRegistration>();
        services.AddScoped<IStoryDirectory, StoryDirectory>();
        services.AddScoped<IAdminStoryGridPort, AdminStoryGridAdapter>();
        services.AddScoped<StoryPresentationComposer>();
        services.AddModuleSchemaMigrator<StoryDbContext>("Story", ModuleSchemaMigrationOrder.Story);
        services.AddDbContext<StoryDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(
                sp.GetRequiredService<ICurrentCommerceContext>(),
                sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(
                options,
                connection,
                StoryDbContext.Schema,
                typeof(StoryDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}
