using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.BuildingBlocks.Localization;
using Tooba.BuildingBlocks.Observability.Correlation;
using Tooba.BuildingBlocks.Observability.Tracing;
using Tooba.BuildingBlocks.Presentation;
using Tooba.BuildingBlocks.Presentation.Errors;
using Tooba.BuildingBlocks.Presentation.ProblemDetails;

namespace Tooba.BuildingBlocks.DependencyInjection;

/// <summary>ثبت canonical foundation مشاهده‌پذیری و ارائهٔ خطا.</summary>
public static class ObservabilityFoundationRegistration
{
    /// <summary>
    /// Correlation، ProblemDetails context، SafeErrorMapper، locale، ApiResponseFactory را ثبت می‌کند.
    /// OpenTelemetry موجود Host را جایگزین نمی‌کند.
    /// </summary>
    public static IServiceCollection AddToobaObservabilityFoundation(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpContextAccessor();
        services.AddOptions<RequestLocaleOptions>();

        services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();
        services.AddSingleton<ICorrelationContext, CorrelationContextAdapter>();
        services.AddSingleton<IModuleCallTracer, ModuleCallTracer>();
        services.AddSingleton<ISafeErrorMapper, SafeErrorMapper>();
        services.AddSingleton<IRequestLocaleResolver, RequestLocaleResolver>();
        services.AddSingleton<IErrorMessageLocalizer, CompositeErrorMessageLocalizer>();

        services.AddSingleton<IProblemDetailsContextProvider, ProblemDetailsContextProvider>();
        services.AddSingleton<ApiResponseFactory>();
        services.AddSingleton<ToobaProblemDetailsFactory>();

        return services;
    }
}
