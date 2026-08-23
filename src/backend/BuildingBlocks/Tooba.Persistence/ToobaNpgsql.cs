using Microsoft.EntityFrameworkCore;
using Tooba.BuildingBlocks;

namespace Tooba.Persistence;

public static class ToobaNpgsql
{
    public const string DesignTimeConnectionVariable = "TOOBA_DESIGN_TIME_CONNECTION";

    public static string DesignTimeConnectionString() =>
        Environment.GetEnvironmentVariable(DesignTimeConnectionVariable)
        ?? "Host=127.0.0.1;Username=tooba;Password=dev-placeholder;Database=tooba_design";

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
            npgsql.UseNodaTime();
        });
        options.UseSnakeCaseNamingConvention();
        options.EnableSensitiveDataLogging(false);
        options.EnableDetailedErrors(false);
    }

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
