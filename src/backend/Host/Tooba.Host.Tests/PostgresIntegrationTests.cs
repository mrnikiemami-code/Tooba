using Testcontainers.PostgreSql;
using Tooba.PlatformProbe.Infrastructure.Persistence;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// دود PostgreSQL واقعی با Testcontainers؛ در نبود Docker تست skip می‌شود نه fail کاذب.
/// </summary>
[Collection("PostgresSerial")]
public sealed class PostgresIntegrationTests : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private bool _dockerAvailable;

    /// <summary>
    /// کانتینر Postgres را در صورت دسترسی Docker بالا می‌آورد.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("tooba_probe")
                .WithUsername("tooba")
                .WithPassword("dev-placeholder")
                .Build();
            await _container.StartAsync();
            _dockerAvailable = true;
        }
        catch (Exception)
        {
            _dockerAvailable = false;
        }
    }

    /// <summary>
    /// کانتینر را آزاد می‌کند.
    /// </summary>
    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    [SkippableFact]
    public async Task Platform_probe_can_create_schema_on_real_postgres()
    {
        Skip.If(!_dockerAvailable || _container is null, "Docker/Testcontainers PostgreSQL is not available.");

        await using var context = PersistenceFoundationTestsHelpers.Create(_container!.GetConnectionString());
        await context.Database.EnsureCreatedAsync();
        var record = PlatformProbePersistence.NewRecord();
        context.Records.Add(record);
        await context.SaveChangesAsync();
        Assert.NotEqual(Guid.Empty, record.Id);
    }
}
