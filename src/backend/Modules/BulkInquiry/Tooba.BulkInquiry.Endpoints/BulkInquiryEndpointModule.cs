using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BulkInquiry.Endpoints.Storefront;

namespace Tooba.BulkInquiry.Endpoints;

/// <summary>ترکیب مالکیت HTTP ماژول BulkInquiry — evacuated from Host/ProductQnA split.</summary>
public static class BulkInquiryEndpointModule
{
    /// <summary>مسیرهای عمومی BulkInquiry را ثبت می‌کند.</summary>
    public static IEndpointRouteBuilder MapBulkInquiryModuleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        BulkInquiryStorefrontEndpoints.Map(app);
        return app;
    }

    /// <summary>ثبت presentation seams. Error catalog owns in Infrastructure.</summary>
    public static IServiceCollection AddBulkInquiryEndpointPresentation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
