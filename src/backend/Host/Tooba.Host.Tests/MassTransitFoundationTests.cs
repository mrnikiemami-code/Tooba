using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tooba.BuildingBlocks;
using Tooba.Host;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// قفل نسخه و مرز بسته‌های messaging بدون نیاز به PostgreSQL.
/// </summary>
public sealed class MassTransitFoundationTests
{
    [Fact]
    public void Host_pins_masstransit_8_5_10_without_rabbitmq_or_v9()
    {
        var root = FindRepoRoot();
        var hostCsproj = File.ReadAllText(Path.Combine(root, "src", "backend", "Host", "Tooba.Host", "Tooba.Host.csproj"));
        var persistenceCsproj = File.ReadAllText(Path.Combine(root, "src", "backend", "BuildingBlocks", "Tooba.Persistence", "Tooba.Persistence.csproj"));
        var blocksCsproj = File.ReadAllText(Path.Combine(root, "src", "backend", "BuildingBlocks", "Tooba.BuildingBlocks", "Tooba.BuildingBlocks.csproj"));

        Assert.Contains("<PackageReference Include=\"MassTransit\" Version=\"8.5.10\" />", hostCsproj, StringComparison.Ordinal);
        Assert.Contains("<PackageReference Include=\"MassTransit.SqlTransport.PostgreSQL\" Version=\"8.5.10\" />", hostCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("MassTransit.RabbitMQ", hostCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RabbitMQ.Client", hostCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MassTransit.EntityFrameworkCore", hostCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Version=\"9.", hostCsproj, StringComparison.Ordinal);
        Assert.DoesNotContain("MassTransit", persistenceCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MassTransit", blocksCsproj, StringComparison.OrdinalIgnoreCase);

        var refs = typeof(IIntegrationEventPublisher).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(refs, a => a.Name!.Contains("MassTransit", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Production_default_is_not_in_process_publisher()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Tooba:Outbox:Enabled"] = "false",
                    ["Tooba:Messaging:Enabled"] = "false",
                    ["Tooba:Messaging:UseInProcessTestDouble"] = "false",
                });
            });
        });

        using var scope = factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>();
        Assert.IsType<MessagingDisabledPublisher>(publisher);
        Assert.IsNotType<InProcessIntegrationEventPublisher>(publisher);
        Assert.Null(factory.Services.GetService<MassTransit.IBusControl>());
    }

    [Fact]
    public void Messaging_validator_rejects_forbidden_rabbitmq_transport()
    {
        var validator = new MessagingOptionsValidator();
        var result = validator.Validate(
            null,
            new MessagingHostOptions
            {
                Enabled = true,
                Transport = "RabbitMq",
                ConnectionReference = "messaging",
                Schema = "transport",
            });

        Assert.False(result.Succeeded);
        Assert.Contains("RabbitMQ", result.FailureMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Host_assembly_does_not_reference_rabbitmq_client()
    {
        var names = Assembly.GetAssembly(typeof(Program))!.GetReferencedAssemblies().Select(a => a.Name!);
        Assert.DoesNotContain(names, n => n.Contains("RabbitMQ", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(names, n => n.Equals("MassTransit", StringComparison.Ordinal));
        Assert.DoesNotContain(names, n => Regex.IsMatch(n, @"MassTransit.*") && n.Contains("9"));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AGENTS.md")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
