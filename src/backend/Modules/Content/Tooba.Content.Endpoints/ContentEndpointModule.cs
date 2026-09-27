using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.Content.Endpoints.Admin;
using Tooba.Content.Endpoints.Storefront;

namespace Tooba.Content.Endpoints;

/// <summary>ترکیب نازک مالکیت HTTP ماژول Content.</summary>
public static class ContentEndpointModule
{
    /// <summary>مسیرهای عمومی و Admin ماژول Content را ثبت می‌کند.</summary>
    public static IEndpointRouteBuilder MapContentModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        ContentStorefrontEndpoints.Map(app);
        ContentEndpoints.MapContentEndpoints(app);
        ContentCategoryEndpoints.MapContentCategoryEndpoints(app);
        ContentAuthorEndpoints.MapContentAuthorEndpoints(app);
        ContentTagEndpoints.MapContentTagEndpoints(app);
        ContentArticleMediaEndpoints.MapContentArticleMediaEndpoints(app);
        ContentArticleCommentEndpoints.MapContentArticleCommentEndpoints(app);
        return app;
    }

    /// <summary>ثبت composers و سرویس‌های presentation مرز Content.</summary>
    public static IServiceCollection AddContentEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<ContentPanelComposer>();
        services.AddScoped<ContentAuthorPanelComposer>();
        services.AddScoped<ContentArticleMediaPanelComposer>();
        return services;
    }
}
