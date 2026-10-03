using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks;
using Tooba.Content.Application.Articles.Ports;
using Tooba.Content.Application.Authors.Ports;
using Tooba.Content.Application.Categories.Ports;
using Tooba.Content.Application.Comments.Ports;
using Tooba.Content.Application.Media.Ports;
using Tooba.Content.Application.Tags.Ports;
using Tooba.Content.Infrastructure.Adapters;
using Tooba.Content.Infrastructure.Directories;
using Tooba.Content.Infrastructure.Persistence;
using Tooba.ModuleContracts;
using Tooba.Persistence;

namespace Tooba.Content.Infrastructure;

/// <summary>ماژول مستقل Content و schema اختصاصی آن.</summary>
public sealed class ContentModule : IToobaModule
{
    /// <inheritdoc />
    public string Name => "Content";

    /// <inheritdoc />
    public void AddServices(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddSingleton<IOutboxModuleRegistration, ContentOutboxRegistration>();
        services.AddScoped<IContentDirectory, ContentDirectory>();
        services.AddScoped<IContentCategoryDirectory, ContentCategoryDirectory>();
        services.AddScoped<IContentAuthorDirectory, ContentAuthorDirectory>();
        services.AddScoped<IContentTagDirectory, ContentTagDirectory>();
        services.AddScoped<IContentArticleMediaDirectory, ContentArticleMediaDirectory>();
        services.AddScoped<IArticleCommentDirectory, ArticleCommentDirectory>();
        services.AddScoped<IContentMediaAssetValidator, ContentMediaAssetValidator>();
        services.AddScoped<IContentArticleGridPort, ContentArticleGridAdapter>();
        services.AddScoped<IContentAuthorGridPort, ContentAuthorGridAdapter>();
        services.AddScoped<Tooba.Content.Contracts.Storefront.IContentStorefrontArticlesPort, ContentStorefrontArticlesAdapter>();
        services.AddScoped<Tooba.Localization.Contracts.Ports.ILanguageReferenceGuard, ContentLanguageReferenceGuard>();
        services.AddModuleSchemaMigrator<ContentDbContext>("Content", ModuleSchemaMigrationOrder.Content);
        services.AddDbContext<ContentDbContext>((sp, options) =>
        {
            var connection = ToobaNpgsql.ResolveForContext(sp.GetRequiredService<ICurrentCommerceContext>(), sp.GetRequiredService<IDatabaseConnectionResolver>());
            ToobaNpgsql.ConfigureModuleContext(options, connection, ContentDbContext.Schema, typeof(ContentDbContext));
            options.AddInterceptors(sp.GetRequiredService<OutboxSaveChangesInterceptor>());
        });
    }
}

/// <summary>ثبت Outbox Content؛ نسخهٔ پایه هنوز رویداد بیرونی منتشر نمی‌کند.</summary>
public sealed class ContentOutboxRegistration : IOutboxModuleRegistration
{
    /// <inheritdoc />
    public string Schema => ContentDbContext.Schema;

    /// <inheritdoc />
    public string TableName => OutboxMessageMapping.TableName;

    /// <inheritdoc />
    public Type DbContextType => typeof(ContentDbContext);

    /// <inheritdoc />
    public IIntegrationEvent? Translate(IDomainEvent domainEvent, EventMetadata metadata) => null;

    /// <inheritdoc />
    public string GetEventTypeName(Type integrationEventType) => throw new InvalidOperationException("Content integration event is not registered.");

    /// <inheritdoc />
    public Type? ResolveEventClrType(string eventTypeName) => null;
}
