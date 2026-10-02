using System.Text.RegularExpressions;
using Xunit;

namespace Tooba.Host.Tests.Architecture;

/// <summary>
/// TB-TMAR-HOST-OUTBOX-AMC-001-W1 — structure/cancellation/options hygiene guard (not certification).
/// </summary>
public sealed class HostOutboxAmcW1GuardTests
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
    public void Outbox_exact_seven_files_namespace_options_and_cancellation()
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
            Assert.DoesNotContain("DbContext", text, StringComparison.Ordinal);
            var types = TopLevelType.Matches(text).Select(m => m.Groups[1].Value).ToArray();
            Assert.True(types.Length == 1, name + " types=" + string.Join(",", types));
            Assert.Equal(Path.GetFileNameWithoutExtension(name), types[0]);
            foreach (Match m in ForeignModuleLayer.Matches(text))
                Assert.Fail("foreign module layer: " + name + ": " + m.Value);
        }

        var dispatcher = Read("src/backend/Host/Tooba.Host/Outbox/OutboxDispatcher.cs");
        Assert.Equal(3, Regex.Matches(dispatcher, @"catch \(OperationCanceledException\) when \(cancellationToken\.IsCancellationRequested\)").Count);
        Assert.Contains("CreateAsyncScope()", dispatcher, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<ICommerceContextAssigner>()", dispatcher, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<IStoreCommerceContextAssigner>()", dispatcher, StringComparison.Ordinal);
        Assert.Contains("GetRequiredService<IIntegrationEventPublisher>()", dispatcher, StringComparison.Ordinal);
        Assert.Contains("1 << Math.Min(message.AttemptCount - 1, 8)", dispatcher, StringComparison.Ordinal);
        Assert.Contains("OutboxErrorSanitizer.Sanitize", dispatcher, StringComparison.Ordinal);
        Assert.DoesNotContain("ex.Message", dispatcher, StringComparison.Ordinal);
        Assert.DoesNotContain("Message.Contains", dispatcher, StringComparison.Ordinal);

        var worker = Read("src/backend/Host/Tooba.Host/Outbox/WorkerCommerceContextFactory.cs");
        Assert.Contains("Worker commerce context could not be reconstructed from registry.", worker, StringComparison.Ordinal);
        Assert.DoesNotContain("زمینهٔ کارگر", worker, StringComparison.Ordinal);

        var storeWorker = Read("src/backend/Host/Tooba.Host/Outbox/WorkerStoreCommerceContextFactory.cs");
        Assert.Contains("StoreContext.Contracts.Current", storeWorker, StringComparison.Ordinal);
        Assert.DoesNotContain("StoreContext.Application", storeWorker, StringComparison.Ordinal);
        Assert.DoesNotContain("StoreContext.Infrastructure", storeWorker, StringComparison.Ordinal);

        var validator = Read("src/backend/Host/Tooba.Host/Outbox/OutboxHostOptionsValidator.cs");
        Assert.Contains("IValidateOptions<OutboxHostOptions>", validator, StringComparison.Ordinal);
        Assert.Contains("PollIntervalSeconds <= 0", validator, StringComparison.Ordinal);
        Assert.Contains("BatchSize <= 0", validator, StringComparison.Ordinal);
        Assert.Contains("RetryBaseDelaySeconds <= 0", validator, StringComparison.Ordinal);
        Assert.Contains("MaxAttempts <= 0", validator, StringComparison.Ordinal);
        Assert.Contains("LockSeconds <= 0", validator, StringComparison.Ordinal);

        var program = Read("src/backend/Host/Tooba.Host/Program.cs");
        Assert.Contains("using Tooba.Host.Outbox;", program, StringComparison.Ordinal);
        Assert.Contains("AddOptions<OutboxHostOptions>()", program, StringComparison.Ordinal);
        Assert.Contains("ValidateOnStart()", program, StringComparison.Ordinal);
        Assert.Contains("IValidateOptions<OutboxHostOptions>", program, StringComparison.Ordinal);
        Assert.Contains("OutboxHostOptionsValidator", program, StringComparison.Ordinal);
        Assert.Contains("GetSection(\"Tooba:Outbox\")", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Outbox_protected_certifications_present_in_sot()
    {
        var state = Read("docs/architecture/tmar-current-state.json");
        Assert.Contains("HOST_OBSERVABILITY_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_MESSAGING_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_HEALTH_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_MULTITENANCY_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_ERRORS_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_SECURITY_AMC_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("HOST_ADMIN_FULLY_CERTIFIED", state, StringComparison.Ordinal);
        Assert.Contains("\"hostOutboxAmc001W1\"", state, StringComparison.Ordinal);
        Assert.Contains("HostOutboxAmcW1GuardTests", state, StringComparison.Ordinal);
        Assert.Contains("EXACT_Tooba.Host.Outbox", state, StringComparison.Ordinal);
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
