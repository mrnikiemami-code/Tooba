using Microsoft.AspNetCore.Diagnostics;
using Tooba.BuildingBlocks.Presentation;

namespace Tooba.Host;

/// <summary>
/// handler نازک استثنای مدیریت‌نشده — ارکستراسیون در IExceptionPresentationService.
/// </summary>
internal sealed class ToobaExceptionHandler : IExceptionHandler
{
    private readonly IExceptionPresentationService _presentation;

    /// <summary>handler سراسری را با سرویس ارائهٔ مرکزی می‌سازد.</summary>
    public ToobaExceptionHandler(IExceptionPresentationService presentation)
        => _presentation = presentation;

    /// <summary>
    /// پاسخ استاندارد می‌نویسد و همیشه true برمی‌گرداند تا pipeline پیش‌فرض جزئیات را لو ندهد.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        await _presentation.WriteAsync(httpContext, exception, cancellationToken);
        return true;
    }
}
