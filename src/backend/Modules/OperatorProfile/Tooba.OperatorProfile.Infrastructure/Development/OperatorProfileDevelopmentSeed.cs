using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.OperatorProfile.Application.Models;
using Tooba.OperatorProfile.Application.Ports;

namespace Tooba.OperatorProfile.Infrastructure.Development;

/// <summary>
/// دانهٔ Development پروفایل اپراتور Admin دمو. مالکیت OperatorProfile؛ Host فقط ActorId می‌دهد.
/// </summary>
public static class OperatorProfileDevelopmentSeed
{
    /// <summary>
    /// پروفایل اپراتور دمو را در صورت نبودن می‌نویسد؛ فقط Development.
    /// </summary>
    public static async Task ApplyAsync(
        IServiceProvider services,
        Guid adminActorUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var environment = services.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            return;
        }

        var directory = services.GetRequiredService<IOperatorProfileDirectory>();
        if (await directory.GetAsync(adminActorUserId, cancellationToken) is not null)
        {
            return;
        }

        await directory.UpsertAsync(
            adminActorUserId,
            new OperatorProfileWrite(
                "اپراتور نمایشی توبا",
                "اپراتور",
                "نمایشی توبا",
                "پروفایل آزمایشی Development برای پنل Admin."),
            cancellationToken);
    }
}
