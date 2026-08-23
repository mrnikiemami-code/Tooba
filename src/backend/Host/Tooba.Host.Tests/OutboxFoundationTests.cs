using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Tooba.BuildingBlocks;
using Tooba.Persistence;
using Tooba.PlatformProbe.Infrastructure;
using Tooba.PlatformProbe.Infrastructure.Events;
using Xunit;

namespace Tooba.Host.Tests;

/// <summary>
/// تست‌های واحد قرارداد Outbox بدون نیاز به PostgreSQL.
/// </summary>
public sealed class OutboxFoundationTests
{
    [Fact]
    public void Domain_event_is_not_automatically_an_integration_event()
    {
        var registration = new PlatformProbeOutboxRegistration();
        var note = new ProbeInternalNoteDomainEvent("internal-only");
        var created = new ProbeRecordCreatedDomainEvent(Guid.NewGuid());
        var meta = EventMetadataFactory.ForDomain("x");

        Assert.Null(registration.Translate(note, meta));
        Assert.NotNull(registration.Translate(created, meta));
        Assert.False(typeof(IIntegrationEvent).IsAssignableFrom(typeof(ProbeInternalNoteDomainEvent)));
        Assert.False(typeof(IDomainEvent).IsAssignableFrom(typeof(ProbeRecordCreatedIntegrationEvent)));
    }

    [Fact]
    public void Worker_reconstructs_tenant_from_message_not_host()
    {
        var options = OutboxTestPlatform.TwoTenants(
            "Host=127.0.0.1;Username=tooba;Password=dev-placeholder;Database=tooba_alpha",
            "Host=127.0.0.1;Username=tooba;Password=dev-placeholder;Database=tooba_bravo");
        var registry = PlatformOptionsValidator.BuildRegistry(options);
        var factory = new WorkerCommerceContextFactory(registry);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = "store-alpha",
            Edition = "SingleStore",
            DeploymentId = "test-outbox",
            EventType = ProbeRecordCreatedIntegrationEvent.EventTypeName,
        };

        var context = factory.FromOutbox(message, "trace-1");
        Assert.Equal("store-alpha", context.Tenant?.TenantId.Value);
        Assert.Equal("tenant-alpha", context.DatabaseConnectionReference.Value);
        Assert.NotEqual("bravo.localhost", context.Tenant?.ResolvedHost);
        Assert.NotEqual("store-bravo", context.Tenant?.TenantId.Value);
    }

    [Fact]
    public void Marketplace_poll_targets_only_marketplace_database()
    {
        var options = OutboxTestPlatform.Marketplace(
            "Host=127.0.0.1;Username=tooba;Password=dev-placeholder;Database=tooba_marketplace");
        var source = new ConfiguredOutboxPollTargetSource(PlatformOptionsValidator.BuildRegistry(options));
        var targets = source.GetTargets();
        Assert.Single(targets);
        Assert.Equal(ToobaEdition.Marketplace, targets[0].Edition);
        Assert.Null(targets[0].TenantId);
        Assert.Equal("marketplace", targets[0].ConnectionReference.Value);
    }

    [Fact]
    public void Single_store_polls_active_tenants_separately_and_skips_disabled()
    {
        var options = OutboxTestPlatform.TwoTenants(
            "Host=127.0.0.1;Username=tooba;Password=dev-placeholder;Database=tooba_alpha",
            "Host=127.0.0.1;Username=tooba;Password=dev-placeholder;Database=tooba_bravo");
        var source = new ConfiguredOutboxPollTargetSource(PlatformOptionsValidator.BuildRegistry(options));
        var targets = source.GetTargets();
        Assert.Equal(2, targets.Count);
        Assert.Contains(targets, t => t.TenantId == "store-alpha");
        Assert.Contains(targets, t => t.TenantId == "store-bravo");
        Assert.DoesNotContain(targets, t => t.TenantId == "store-disabled");
        Assert.NotEqual(targets[0].ConnectionReference, targets[1].ConnectionReference);
    }

    [Fact]
    public void Last_error_omits_secrets_stack_and_payload()
    {
        var ex = new InvalidOperationException("Password=super-secret at Tooba.Host.OutboxDispatcher.Dispatch {\"recordId\":1}");
        var sanitized = OutboxErrorSanitizer.Sanitize(ex);
        Assert.DoesNotContain("super-secret", sanitized, StringComparison.Ordinal);
        Assert.DoesNotContain("at Tooba.Host", sanitized, StringComparison.Ordinal);
        Assert.DoesNotContain("recordId", sanitized, StringComparison.Ordinal);
        Assert.True(sanitized.Length <= OutboxErrorSanitizer.MaxLength);
    }

    [Fact]
    public void Serializer_rejects_unknown_event_type_without_clr_gettype()
    {
        var serializer = new JsonIntegrationEventSerializer([new PlatformProbeOutboxRegistration()]);
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = "unknown.event.v1",
            Payload = "{}",
            Edition = "SingleStore",
            DeploymentId = "x",
            OccurredAt = NodaTime.SystemClock.Instance.GetCurrentInstant(),
        };
        Assert.Throws<InvalidOperationException>(() => serializer.Deserialize(message));
    }

    [Fact]
    public void Building_blocks_and_persistence_have_no_broker_package()
    {
        var root = FindRepoRoot();
        var persistenceCsproj = File.ReadAllText(Path.Combine(root, "src", "backend", "BuildingBlocks", "Tooba.Persistence", "Tooba.Persistence.csproj"));
        var blocksCsproj = File.ReadAllText(Path.Combine(root, "src", "backend", "BuildingBlocks", "Tooba.BuildingBlocks", "Tooba.BuildingBlocks.csproj"));
        foreach (var text in new[] { persistenceCsproj, blocksCsproj })
        {
            Assert.DoesNotContain("MassTransit", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("RabbitMQ", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("EasyNetQ", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("NServiceBus", text, StringComparison.OrdinalIgnoreCase);
        }

        var refs = typeof(IIntegrationEventPublisher).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(refs, a => a.Name!.Contains("MassTransit", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refs, a => a.Name!.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Ready_does_not_depend_on_empty_outbox()
    {
        await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Tooba:Outbox:Enabled"] = "false",
                });
            });
        });
        var client = factory.CreateClient();
        var ready = await client.GetAsync("/ready");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        var body = await ready.Content.ReadAsStringAsync();
        Assert.DoesNotContain("outbox", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pending", body, StringComparison.OrdinalIgnoreCase);
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
