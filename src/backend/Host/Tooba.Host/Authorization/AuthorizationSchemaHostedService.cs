using Tooba.BuildingBlocks;

namespace Tooba.Host;

/// <summary>
/// در استارت Host، schema را فقط وقتی ApplySchemaOnStartup روشن باشد اعمال می‌کند.
/// تولید با مقدار پیش‌فرض false هر بار schema را بازنویسی نمی‌کند.
/// </summary>
internal sealed class AuthorizationSchemaHostedService : IHostedService
{
    private readonly IAuthorizationSchemaBootstrapper _bootstrapper;

    /// <summary>
    /// hosted service را روی bootstrapper موجود Host می‌سازد.
    /// </summary>
    public AuthorizationSchemaHostedService(IAuthorizationSchemaBootstrapper bootstrapper) =>
        _bootstrapper = bootstrapper;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) =>
        _bootstrapper.BootstrapIfConfiguredAsync(cancellationToken);

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
