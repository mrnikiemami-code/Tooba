using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-OUTBOX-AMC-001-W2-CERT — durable certification of Host/Outbox platform boundary.
/// </summary>
public sealed class HostOutboxAmcCertGuardTests
{
    private static readonly string[] ExpectedFiles =
    [
        "ConfiguredOutboxPollTargetSource.cs",
        "OutboxDispatcher.cs",
        "OutboxDispatcherHostedService.cs",
        "OutboxHostOptions.cs",
        "OutboxHostOptionsValidator.cs",
        "WorkerCommerceContextFactory.cs",
        "WorkerStoreCommerceContextFactory.cs",
    ];

    private static readonly Regex TopLevelType = new(
        @"^\s*(?:internal\s+|public\s+|file\s+)?(?:sealed\s+|static\s+|abstract\s+)*(?:partial\s+)?(?:class|record|struct|enum|interface)\s+(\w+)",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    private static readonly Regex ForeignModuleLayer = new(
        @"Tooba\.(Catalog|Party|AccessControl|Identity|Order|Offer|Promotion|Returns|Settlement|Notification|Support|Story|Payment|Inventory|Cart|Persistence)\.(Application|Domain|Infrastructure|Persistence)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    [Fact]
    public void Outbox_certified_exact_tree_cancellation_options_and_boundaries()
    {
        var dir = Dir("src/backend/Host/Tooba.Host/Outbox");
        Assert.True(Directory.Exists(dir));
        Assert.Empty(Directory.GetDirectories(dir));
        var files = Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(ExpectedFiles, files);

        foreach (var name in ExpectedFiles)
        {
            var text = File.ReadAllText(Path.Combine(dir, name!));
            Assert.Contains("namespace Tooba.Host.Outbox;", text, StringComparison.Ordinal);
            Assert.DoesNotContain("namespace Tooba.Host;", text, StringComparison.Ordinal);
            Assert.DoesNotContain("TypeForwardedTo", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MassTransit", text, StringComparison.Ordinal);
            Assert.DoesNotContain("IBus", text, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            var types = TopLevelType.Matches(text).Select(m => m.Groups[1].Value).ToArray();
            Assert.True(types.Length == 1, name + " types=" + string.Join(",", types));
            Assert.Equal(Path.GetFileNameWithoutExtension(name), types[0]);
            foreach (Match m in ForeignModuleLayer.Matches(text))
                Assert.Fail("foreign module layer: " + name + ": " + m.Value);
        }

        var dispatcher = Read("src/backend/Host/Tooba.Host/Outbox/OutboxDispatcher.cs");
        Assert.Equal(3, Regex.Matches(dispatcher, @"catch \(OperationCanceledException\) when \(cancellationToken\.IsCancellationRequested\)").Count);
        Assert.Contains("IServiceScopeFactory", dispatcher, StringComparison.Ordinal);
        Assert.Contains("CreateAsyncScope()", dispatcher, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<ICommerceContextAssigner>()", dispatcher, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<IStoreCommerceContextAssigner>()", dispatcher, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<IIntegrationEventPublisher>()", dispatcher, StringComparison.Ordinal);
        Assert.Contains("1 << Math.Min(message.AttemptCount - 1, 8)", dispatcher, StringComparison.Ordinal);
        Assert.Contains("OutboxErrorSanitizer.Sanitize", dispatcher, StringComparison.Ordinal);
        Assert.Contains("tooba.outbox.tenant_failures", dispatcher, StringComparison.Ordinal);
        Assert.Contains("tooba.outbox.retries", dispatcher, StringComparison.Ordinal);
        Assert.Contains("tooba.outbox.dead_letters", dispatcher, StringComparison.Ordinal);
        Assert.Contains("tooba.outbox.processed", dispatcher, StringComparison.Ordinal);
        Assert.Contains("MessagingCorrelation", dispatcher, StringComparison.Ordinal);
        Assert.Contains("CorrelationIdContext", dispatcher, StringComparison.Ordinal);
        Assert.Contains("ToobaTraceEnricher", dispatcher, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", dispatcher, StringComparison.Ordinal);
        Assert.DoesNotContain("Message.Contains", dispatcher, StringComparison.Ordinal);
        Assert.DoesNotContain("Message.StartsWith", dispatcher, StringComparison.Ordinal);

        var hosted = Read("src/backend/Host/Tooba.Host/Outbox/OutboxDispatcherHostedService.cs");
        Assert.Equal(2, Regex.Matches(hosted, @"catch \(OperationCanceledException\) when \(stoppingToken\.IsCancellationRequested\)").Count);

        var options = Read("src/backend/Host/Tooba.Host/Outbox/OutboxHostOptions.cs");
        Assert.Contains("Enabled { get; set; } = true", options, StringComparison.Ordinal);
        Assert.Contains("PollIntervalSeconds { get; set; } = 2", options, StringComparison.Ordinal);
        Assert.Contains("BatchSize { get; set; } = 20", options, StringComparison.Ordinal);
        Assert.Contains("RetryBaseDelaySeconds { get; set; } = 2", options, StringComparison.Ordinal);
        Assert.Contains("MaxAttempts { get; set; } = 5", options, StringComparison.Ordinal);
        Assert.Contains("LockSeconds { get; set; } = 30", options, StringComparison.Ordinal);

        var validator = Read("src/backend/Host/Tooba.Host/Outbox/OutboxHostOptionsValidator.cs");
        Assert.Contains("IValidateOptions<OutboxHostOptions>", validator, StringComparison.Ordinal);
        Assert.Contains("PollIntervalSeconds <= 0", validator, StringComparison.Ordinal);
        Assert.Contains("BatchSize <= 0", validator, StringComparison.Ordinal);
        Assert.Contains("RetryBaseDelaySeconds <= 0", validator, StringComparison.Ordinal);
        Assert.Contains("MaxAttempts <= 0", validator, StringComparison.Ordinal);
        Assert.Contains("LockSeconds <= 0", validator, StringComparison.Ordinal);

        var worker = Read("src/backend/Host/Tooba.Host/Outbox/WorkerCommerceContextFactory.cs");
        Assert.Contains("Worker commerce context could not be reconstructed from registry.", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("زمینهٔ کارگر", worker, StringComparison.Ordinal);
        Assert.Contains("Marketplace outbox worker has no connection reference.", worker, StringComparison.Ordinal);
        Assert.Contains("Outbox tenant could not be reconstructed from registry.", worker, StringComparison.Ordinal);

        var storeWorker = Read("src/backend/Host/Tooba.Host/Outbox/WorkerStoreCommerceContextFactory.cs");
        Assert.Contains("StoreContext.Contracts.Current", storeWorker, StringComparison.Ordinal);
        Assert.Contains("DeploymentStoreCommerce", storeWorker, StringComparison.Ordinal);
        Assert.DoesNotContain("StoreContext.Application", storeWorker, StringComparison.Ordinal);
        Assert.DoesNotContain("StoreContext.Infrastructure", storeWorker, StringComparison.Ordinal);

        var poll = Read("src/backend/Host/Tooba.Host/Outbox/ConfiguredOutboxPollTargetSource.cs");
        Assert.Contains("TenantStatus.Active", poll, StringComparison.Ordinal);
        Assert.Contains("ToobaEdition.Marketplace", poll, StringComparison.Ordinal);
        Assert.Contains("ToobaEdition.SingleStore", poll, StringComparison.Ordinal);

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("using Tooba.Host.Outbox;", program, StringComparison.Ordinal);
        Assert.Contains("AddOptions<OutboxHostOptions>()", program, StringComparison.Ordinal);
        Assert.Contains("GetSection(\"Tooba:Outbox\")", program, StringComparison.Ordinal);
        Assert.Contains("ValidateOnStart()", program, StringComparison.Ordinal);
        Assert.Contains("IValidateOptions<OutboxHostOptions>, OutboxHostOptionsValidator", program, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<IOutboxDispatcherStore, NpgsqlOutboxDispatcherStore>()", program, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<IOutboxPollTargetSource, ConfiguredOutboxPollTargetSource>()", program, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<WorkerCommerceContextFactory>()", program, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<IWorkerCommerceContextFactory>", program, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<WorkerStoreCommerceContextFactory>()", program, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<IWorkerStoreCommerceContextFactory>", program, StringComparison.Ordinal);
        Assert.Contains("AddSingleton<OutboxDispatcher>()", program, StringComparison.Ordinal);
        Assert.Contains("AddHostedService<OutboxDispatcherHostedService>()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Sot_certifies_outbox_and_preserves_prior_host_certs()
    {
        var sot = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_OUTBOX_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_OUTBOX_PLATFORM_BOUNDARY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("\"hostOutboxAmc001W2Cert\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"implementationCommit\":  \"382ef10af3a5eb49f519e49cb399809b19844bbc\"", sot, StringComparison.Ordinal);
        Assert.Contains("USER_REVIEW_HOST_OUTBOX_AMC_001_W2_CERT", sot, StringComparison.Ordinal);
        Assert.Contains("REQUESTED_OCE_PROPAGATES_NO_RETRY_DEADLETTER_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("LEGITIMATE_PER_MESSAGE_WORKER_SCOPE_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("ANTI_SPOOF_REGISTRY_AUTHORITY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HostOutboxAmcCertGuardTests", sot, StringComparison.Ordinal);
        Assert.Contains("HostOutboxAmcW1GuardTests", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_OBSERVABILITY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_MESSAGING_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_HEALTH_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_MULTITENANCY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ERRORS_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_SECURITY_AMC_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", sot, StringComparison.Ordinal);
        Assert.Contains("\"lastAcceptedCommit\":  \"382ef10af3a5eb49f519e49cb399809b19844bbc\"", sot, StringComparison.Ordinal);
        Assert.Contains("\"latestAcceptedImplementationWave\":  \"TB-TMAR-HOST-OUTBOX-AMC-001-W1\"", sot, StringComparison.Ordinal);
    }

    private static string Dir(string relative) => Path.Combine(Repo(), relative.Replace('/', Path.DirectorySeparatorChar));

    private static string Read(string relative) => File.ReadAllText(Repo(relative));

    private static string Repo(string? relative = null)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "AGENTS.md")))
            {
                return relative is null
                    ? directory.FullName
                    : Path.Combine(directory.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("repo root not found");
    }
}
