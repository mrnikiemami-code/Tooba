using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;

namespace Tooba.Persistence;

/// <summary>
/// کمک‌های مشترک Npgsql/EF برای DbContextهای ماژول: یک schema، تاریخچهٔ مهاجرت جدا، بدون mega-DbContext.
/// اتصال از <see cref="ICurrentCommerceContext"/> و <see cref="IDatabaseConnectionResolver"/> می‌آید نه از Host خام در ماژول.
/// DataSource/NodaTime سراسری روی Npgsql ثبت نمی‌شود تا Dapper MassTransit timestamptz را Instant نبیند.
/// Instantهای ماژول با تبدیل DateTimeOffset نگاشت می‌شوند.
/// </summary>
public static class ToobaNpgsql
{
    /// <summary>
    /// متغیر محیطی اتصال design-time؛ فقط برای ابزار EF، نه runtime تولید.
    /// </summary>
    public const string DesignTimeConnectionVariable = "TOOBA_DESIGN_TIME_CONNECTION";

    /// <summary>
    /// رشتهٔ اتصال design-time. مقدار پیش‌فرض محلی است و credential تولید نیست.
    /// </summary>
    public static string DesignTimeConnectionString() =>
        Environment.GetEnvironmentVariable(DesignTimeConnectionVariable)
        ?? "Host=127.0.0.1;Username=tooba;Password=dev-placeholder;Database=tooba_design";

    /// <summary>
    /// پیکربندی استاندارد یک DbContext ماژول: Npgsql + NodaTime + snake_case + جدول مهاجرت داخل همان schema.
    /// </summary>
    /// <param name="options">سازندهٔ گزینه‌های EF.</param>
    /// <param name="connectionString">رشتهٔ اتصال resolveشده؛ نباید لاگ حساس شود.</param>
    /// <param name="schema">مالکیت دادهٔ همین ماژول؛ join بین‌ماژولی ممنوع است.</param>
    /// <param name="migrationsAssemblyMarker">نوع نشانگر اسمبلی مهاجرت‌ها.</param>
    public static void ConfigureModuleContext(
        DbContextOptionsBuilder options,
        string connectionString,
        string schema,
        Type migrationsAssemblyMarker)
    {
        options.UseNpgsql(connectionString, npgsql =>
        {
            npgsql.MigrationsHistoryTable("__ef_migrations_history", schema);
            npgsql.MigrationsAssembly(migrationsAssemblyMarker.Assembly.GetName().Name);
        });
        options.UseSnakeCaseNamingConvention();
        options.EnableSensitiveDataLogging(false);
        options.EnableDetailedErrors(false);
    }

    /// <summary>
    /// اتصال DbContext را از زمینهٔ تجارت درخواست استخراج می‌کند.
    /// </summary>
    /// <exception cref="PlatformHttpException">اگر زمینه هنوز resolve نشده (۵۰۳ edition).</exception>
    public static string ResolveForContext(
        ICurrentCommerceContext commerce,
        IDatabaseConnectionResolver resolver)
    {
        var current = commerce.Current
            ?? throw new PlatformHttpException(
                503,
                "Service Unavailable",
                "platform.edition.unconfigured");
        return resolver.Resolve(current.DatabaseConnectionReference);
    }
}
