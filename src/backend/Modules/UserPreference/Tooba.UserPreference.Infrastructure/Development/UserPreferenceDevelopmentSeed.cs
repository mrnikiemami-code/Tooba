using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Tooba.UserPreference.Application;
using UserPreferenceEntity = Tooba.UserPreference.Domain.UserPreference;

namespace Tooba.UserPreference.Infrastructure.Development;

/// <summary>
/// دانهٔ Development ترجیح locale مهمان/ادمین. مالکیت UserPreference؛ Host فقط شناسه‌ها را می‌دهد.
/// </summary>
public static class UserPreferenceDevelopmentSeed
{
    /// <summary>
    /// ترجیح locale را برای مهمان و در صورت وجود برای ادمین، idempotent می‌نویسد؛ فقط Development.
    /// </summary>
    public static async Task ApplyAsync(
        IServiceProvider services,
        Guid guestActorUserId,
        Guid? adminActorUserId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);

        var environment = services.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            return;
        }

        var directory = services.GetRequiredService<IUserPreferenceDirectory>();
        if (await directory.GetAsync(guestActorUserId, cancellationToken) is null)
        {
            await directory.UpsertAsync(
                guestActorUserId,
                new UserPreferenceWrite(UserPreferenceEntity.LocaleFa),
                cancellationToken);
        }

        if (adminActorUserId is Guid adminId
            && await directory.GetAsync(adminId, cancellationToken) is null)
        {
            await directory.UpsertAsync(
                adminId,
                new UserPreferenceWrite(UserPreferenceEntity.LocaleFa),
                cancellationToken);
        }
    }
}
